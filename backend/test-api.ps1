## ============================================================
##  Smart Locker API — Test Script (PowerShell)
##  Chạy: .\test-api.ps1
##  Yêu cầu: API đang chạy tại http://localhost:8080
## ============================================================

$BASE = "http://localhost:8080"
$PASS = 0; $FAIL = 0

function Print-Result($name, $ok, $detail = "") {
    if ($ok) {
        Write-Host "  [PASS] $name" -ForegroundColor Green
        $script:PASS++
    } else {
        Write-Host "  [FAIL] $name — $detail" -ForegroundColor Red
        $script:FAIL++
    }
}

function Invoke-API($method, $url, $body = $null, $token = $null) {
    $headers = @{ "Content-Type" = "application/json" }
    if ($token) { $headers["Authorization"] = "Bearer $token" }
    try {
        if ($body) {
            $resp = Invoke-WebRequest -Method $method -Uri $url -Headers $headers `
                        -Body ($body | ConvertTo-Json) -ErrorAction Stop
        } else {
            $resp = Invoke-WebRequest -Method $method -Uri $url -Headers $headers `
                        -ErrorAction Stop
        }
        return @{ Status = $resp.StatusCode; Body = $resp.Content | ConvertFrom-Json }
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        return @{ Status = $code; Body = $null; Error = $_.ToString() }
    }
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  SMART LOCKER API — TEST SUITE" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# ── 1. Health Check ───────────────────────────────────────────
Write-Host "[ 1 ] Health Check" -ForegroundColor Yellow
$r = Invoke-API "GET" "$BASE/health"
Print-Result "GET /health → 200" ($r.Status -eq 200) $r.Error

# ── 2. Auth — đăng nhập sai ──────────────────────────────────
Write-Host ""
Write-Host "[ 2 ] Auth — Login" -ForegroundColor Yellow

$r = Invoke-API "POST" "$BASE/api/auth/login" @{ username="admin"; password="wrong" }
Print-Result "POST /api/auth/login sai password → 401" ($r.Status -eq 401) $r.Error

# Auth — đăng nhập đúng
$r = Invoke-API "POST" "$BASE/api/auth/login" @{ username="admin"; password="admin123" }
Print-Result "POST /api/auth/login đúng → 200 + token" ($r.Status -eq 200 -and $r.Body.token -ne $null) $r.Error
$TOKEN = $r.Body.token
Write-Host "    Token: $($TOKEN.Substring(0, [Math]::Min(40,$TOKEN.Length)))..." -ForegroundColor DarkGray

# ── 3. Lockers — không có token ──────────────────────────────
Write-Host ""
Write-Host "[ 3 ] Lockers — Auth guard" -ForegroundColor Yellow
$r = Invoke-API "GET" "$BASE/api/lockers"
Print-Result "GET /api/lockers không có token → 401" ($r.Status -eq 401) $r.Error

# ── 4. Lockers — có token ────────────────────────────────────
Write-Host ""
Write-Host "[ 4 ] Lockers — Lấy danh sách" -ForegroundColor Yellow
$r = Invoke-API "GET" "$BASE/api/lockers" -token $TOKEN
Print-Result "GET /api/lockers → 200 + data[]" ($r.Status -eq 200 -and $r.Body.data.Count -gt 0) $r.Error
$LOCKER_ID = $r.Body.data[0].id
Write-Host "    Tủ đầu tiên: ID=$LOCKER_ID  Name=$($r.Body.data[0].name)  Status=$($r.Body.data[0].status)" -ForegroundColor DarkGray

# ── 5. ESP32 — polling khi chưa có lệnh ─────────────────────
Write-Host ""
Write-Host "[ 5 ] ESP32 — Polling (chưa có lệnh)" -ForegroundColor Yellow
$r = Invoke-API "GET" "$BASE/api/command/pending/$LOCKER_ID"
Print-Result "GET /api/command/pending/$LOCKER_ID → 204 No Content" ($r.Status -eq 204) $r.Error

# ── 6. Thuê tủ ───────────────────────────────────────────────
Write-Host ""
Write-Host "[ 6 ] Thuê tủ" -ForegroundColor Yellow
$r = Invoke-API "POST" "$BASE/api/lockers/rent" @{ lockerId=$LOCKER_ID } $TOKEN
Print-Result "POST /api/lockers/rent → 200 success" ($r.Status -eq 200 -and $r.Body.success -eq $true) $r.Error

# Thuê lại tủ đang occupied
$r2 = Invoke-API "POST" "$BASE/api/lockers/rent" @{ lockerId=$LOCKER_ID } $TOKEN
Print-Result "POST /api/lockers/rent tủ occupied → 400" ($r2.Status -eq 400) $r2.Error

# ── 7. Kiểm tra tủ đã chuyển sang occupied ───────────────────
Write-Host ""
Write-Host "[ 7 ] Trạng thái tủ sau khi thuê" -ForegroundColor Yellow
$r = Invoke-API "GET" "$BASE/api/lockers" -token $TOKEN
$locker = $r.Body.data | Where-Object { $_.id -eq $LOCKER_ID }
Print-Result "Tủ $LOCKER_ID status = occupied" ($locker.status -eq "occupied") "Status=$($locker.status)"

# ── 8. ESP32 — polling sau khi thuê (có lệnh pending) ────────
Write-Host ""
Write-Host "[ 8 ] ESP32 — Polling (có lệnh)" -ForegroundColor Yellow
$r = Invoke-API "GET" "$BASE/api/command/pending/$LOCKER_ID"
Print-Result "GET /api/command/pending/$LOCKER_ID → 200 + action=open" `
    ($r.Status -eq 200 -and $r.Body.action -eq "open") $r.Error
$CMD_ID = $r.Body.id
Write-Host "    Command: ID=$CMD_ID  Action=$($r.Body.action)" -ForegroundColor DarkGray

# ── 9. ESP32 — báo done ──────────────────────────────────────
Write-Host ""
Write-Host "[ 9 ] ESP32 — Báo done" -ForegroundColor Yellow
$r = Invoke-API "POST" "$BASE/api/command/done/$CMD_ID"
Print-Result "POST /api/command/done/$CMD_ID → 200 success" ($r.Status -eq 200 -and $r.Body.success -eq $true) $r.Error

# done lần 2 (command đã done)
$r2 = Invoke-API "POST" "$BASE/api/command/done/$CMD_ID"
Print-Result "POST /api/command/done/$CMD_ID lần 2 → 404 (đã done)" ($r2.Status -eq 404) $r2.Error

# ── 10. Kiểm tra tủ quay lại available ───────────────────────
Write-Host ""
Write-Host "[ 10 ] Trạng thái tủ sau khi done" -ForegroundColor Yellow
$r = Invoke-API "GET" "$BASE/api/lockers" -token $TOKEN
$locker = $r.Body.data | Where-Object { $_.id -eq $LOCKER_ID }
Print-Result "Tủ $LOCKER_ID status = available" ($locker.status -eq "available") "Status=$($locker.status)"

# ── 11. Lịch sử thuê ─────────────────────────────────────────
Write-Host ""
Write-Host "[ 11 ] Lịch sử thuê" -ForegroundColor Yellow
$r = Invoke-API "GET" "$BASE/api/lockers/history" -token $TOKEN
Print-Result "GET /api/lockers/history → 200 + có bản ghi" ($r.Status -eq 200 -and $r.Body.data.Count -gt 0) $r.Error
if ($r.Body.data.Count -gt 0) {
    $h = $r.Body.data[0]
    Write-Host "    Lịch sử: Tủ=$($h.lockerName)  Status=$($h.status)  Thuê=$($h.rentedAt)" -ForegroundColor DarkGray
}

# ── 12. ESP32 polling sau done → 204 lại ─────────────────────
Write-Host ""
Write-Host "[ 12 ] ESP32 — Polling sau done (không còn lệnh)" -ForegroundColor Yellow
$r = Invoke-API "GET" "$BASE/api/command/pending/$LOCKER_ID"
Print-Result "GET /api/command/pending/$LOCKER_ID → 204 (sạch lệnh)" ($r.Status -eq 204) $r.Error

# ── Kết quả ──────────────────────────────────────────────────
Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
$total = $PASS + $FAIL
Write-Host "  KẾT QUẢ: $PASS/$total PASSED" -ForegroundColor $(if ($FAIL -eq 0) { "Green" } else { "Yellow" })
if ($FAIL -gt 0) {
    Write-Host "  FAILED:  $FAIL test(s)" -ForegroundColor Red
}
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
