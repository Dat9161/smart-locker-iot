const STORAGE_KEYS = { token: "smartLockerToken", user: "smartLockerUser" };

const API_BASE = "http://localhost:5167";

// ── Bootstrap ────────────────────────────────────────────────
document.addEventListener("DOMContentLoaded", () => {
  bindLogout();
  const page = location.pathname.split("/").pop().toLowerCase();

  if (!page || page === "index.html") { setupLoginPage(); return; }

  if (!localStorage.getItem(STORAGE_KEYS.token)) {
    location.href = "index.html"; return;
  }

  if (page === "dashboard.html") setupDashboardPage();
  if (page === "history.html")   setupHistoryPage();
});

// ── Auth ─────────────────────────────────────────────────────
function setupLoginPage() {
  if (localStorage.getItem(STORAGE_KEYS.token)) { location.href = "dashboard.html"; return; }

  const tabLogin    = document.getElementById("tabLogin");
  const tabRegister = document.getElementById("tabRegister");
  const loginForm   = document.getElementById("loginForm");
  const regForm     = document.getElementById("registerForm");

  tabLogin.addEventListener("click", () => {
    tabLogin.classList.add("active"); tabRegister.classList.remove("active");
    loginForm.classList.remove("d-none"); regForm.classList.add("d-none");
  });

  tabRegister.addEventListener("click", () => {
    tabRegister.classList.add("active"); tabLogin.classList.remove("active");
    regForm.classList.remove("d-none"); loginForm.classList.add("d-none");
  });

  const loginAlert = document.getElementById("loginAlert");
  const loginBtn   = document.getElementById("loginButton");

  loginForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const username = document.getElementById("loginUsername").value.trim();
    const password = document.getElementById("loginPassword").value.trim();
    if (!username || !password) { showAlert(loginAlert, "Vui lòng nhập đầy đủ.", "danger"); return; }

    setLoading(loginBtn, true, "Đang đăng nhập...");
    try {
      const res = await api.post("/api/auth/login", { username, password });
      localStorage.setItem(STORAGE_KEYS.token, res.data.token);
      localStorage.setItem(STORAGE_KEYS.user, JSON.stringify({ fullName: res.data.fullName }));
      showAlert(loginAlert, "Đăng nhập thành công!", "success");
      setTimeout(() => location.href = "dashboard.html", 600);
    } catch (err) {
      showAlert(loginAlert, err.message, "danger");
    } finally {
      setLoading(loginBtn, false, "Đăng nhập");
    }
  });

  const regAlert = document.getElementById("registerAlert");
  const regBtn   = document.getElementById("registerButton");

  regForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const fullName = document.getElementById("registerFullName").value.trim();
    const username = document.getElementById("registerUsername").value.trim();
    const password = document.getElementById("registerPassword").value.trim();
    if (!fullName || !username || !password) { showAlert(regAlert, "Vui lòng nhập đầy đủ.", "danger"); return; }
    if (password.length < 6) { showAlert(regAlert, "Mật khẩu tối thiểu 6 ký tự.", "danger"); return; }

    setLoading(regBtn, true, "Đang đăng ký...");
    try {
      await api.post("/api/auth/register", { fullName, username, password });
      showAlert(regAlert, "Đăng ký thành công! Vui lòng đăng nhập.", "success");
      setTimeout(() => tabLogin.click(), 1200);
    } catch (err) {
      showAlert(regAlert, err.message, "danger");
    } finally {
      setLoading(regBtn, false, "Đăng ký");
    }
  });
}

// ── Dashboard ────────────────────────────────────────────────
async function setupDashboardPage() {
  let lockers = await api.get("/api/lockers").then(r => r.data);
  let selectedId = null;
  const returningIds = new Set();
  const watcherTimers = {};

  const grid        = document.getElementById("lockerGrid");
  const rentModal   = document.getElementById("rentModal");
  const returnModal = document.getElementById("returnModal");

  // Poll server mỗi 3s cho đến khi tủ lockerId về "available"
  function startReturnWatcher(lockerId) {
    if (watcherTimers[lockerId]) return; // đã có watcher rồi
    watcherTimers[lockerId] = setInterval(async () => {
      try {
        const fresh = await api.get("/api/lockers").then(r => r.data);
        const locker = fresh.find(l => l.id === lockerId);
        if (locker?.status === "available") {
          clearInterval(watcherTimers[lockerId]);
          delete watcherTimers[lockerId];
          returningIds.delete(lockerId);
          lockers = fresh;
          render();
          toast(`${locker.name} đã trả thành công!`, "success");
        }
      } catch { /* bỏ qua lỗi mạng, thử lại lần sau */ }
    }, 3000);
  }

  function render() {
    const avail = lockers.filter(l => l.status === "available").length;
    document.getElementById("totalLockers").textContent     = lockers.length;
    document.getElementById("availableLockers").textContent = avail;
    document.getElementById("occupiedLockers").textContent  = lockers.length - avail;

    grid.innerHTML = lockers.map(l => {
      const isAvail   = l.status === "available";
      const returning = returningIds.has(l.id);
      const cardClass = isAvail ? "available" : (returning ? "returning" : "occupied");
      return `
        <div class="locker-card ${cardClass}">
          <span class="locker-icon">${isAvail ? "🔓" : "🔒"}</span>
          <span class="locker-number">${l.name}</span>
          <span class="locker-status">${isAvail ? "Đang trống" : returning ? "Đang mở để lấy đồ..." : "Đang sử dụng"}</span>
          ${isAvail
            ? `<button class="btn-action btn-rent" data-id="${l.id}">+ Thuê tủ</button>`
            : returning
              ? `<button class="btn-action" disabled>⏳ Đang xử lý...</button>`
              : `<button class="btn-action btn-return" data-id="${l.id}">↩ Trả tủ</button>`
          }
        </div>`;
    }).join("");

    grid.querySelectorAll(".btn-rent").forEach(btn =>
      btn.addEventListener("click", () => {
        selectedId = Number(btn.dataset.id);
        document.getElementById("rentLockerName").textContent = lockers.find(l => l.id === selectedId)?.name;
        document.getElementById("rentPinInput").value = "";
        showModal(rentModal);
      })
    );

    grid.querySelectorAll(".btn-return").forEach(btn =>
      btn.addEventListener("click", () => {
        selectedId = Number(btn.dataset.id);
        document.getElementById("returnLockerName").textContent = lockers.find(l => l.id === selectedId)?.name;
        document.getElementById("returnPinInput").value = "";
        showModal(returnModal);
      })
    );
  }

  render();

  // Rent
  document.getElementById("rentCancelBtn").addEventListener("click", () => {
    hideModal(rentModal);
    document.getElementById("rentPinInput").value = "";
  });
  const rentConfirm = document.getElementById("rentConfirmBtn");
  rentConfirm.addEventListener("click", async () => {
    const pin = document.getElementById("rentPinInput").value.trim();
    if (!/^\d{4,6}$/.test(pin)) {
      toast("PIN phải là 4-6 chữ số.", "error"); return;
    }
    setLoading(rentConfirm, true, "Đang xử lý...");
    try {
      await api.post("/api/lockers/rent", { lockerId: selectedId, pin });
      lockers = lockers.map(l => l.id === selectedId ? { ...l, status: "occupied" } : l);
      hideModal(rentModal);
      document.getElementById("rentPinInput").value = "";
      render();
      toast("Thuê tủ thành công! Nhập PIN để mở tủ.", "success");
    } catch (err) {
      toast(err.message, "error");
    } finally {
      setLoading(rentConfirm, false, "Thuê tủ");
    }
  });

  // Return — nhập PIN để trả tủ
  document.getElementById("returnCancelBtn").addEventListener("click", () => {
    hideModal(returnModal);
    document.getElementById("returnPinInput").value = "";
  });
  const returnConfirm = document.getElementById("returnConfirmBtn");
  returnConfirm.addEventListener("click", async () => {
    const pin = document.getElementById("returnPinInput").value.trim();
    if (!/^\d{4,6}$/.test(pin)) {
      toast("PIN phải là 4-6 chữ số.", "error"); return;
    }
    setLoading(returnConfirm, true, "Đang xử lý...");
    try {
      await api.post("/api/lockers/return", { lockerId: selectedId, pin });
      returningIds.add(selectedId);
      hideModal(returnModal);
      document.getElementById("returnPinInput").value = "";
      render();
      startReturnWatcher(selectedId);
      toast("PIN đúng! Tủ đang mở, vui lòng lấy đồ ra.", "success");
    } catch (err) {
      toast(err.message, "error");
    } finally {
      setLoading(returnConfirm, false, "Trả tủ");
    }
  });

  // Close modal khi click ngoài
  [rentModal, returnModal].forEach(m => m.addEventListener("click", e => { if (e.target === m) hideModal(m); }));
}

// ── History ──────────────────────────────────────────────────
async function setupHistoryPage() {
  const tbody = document.getElementById("historyTableBody");
  const count = document.getElementById("historyCount");

  try {
    const data = await api.get("/api/lockers/history").then(r => r.data);
    count.textContent = `${data.length} bản ghi`;

    if (data.length === 0) {
      tbody.innerHTML = `<tr><td colspan="4" style="text-align:center;color:var(--muted);padding:32px">Chưa có lịch sử thuê tủ.</td></tr>`;
      return;
    }

    tbody.innerHTML = data.map(item => `
      <tr>
        <td><strong>${item.lockerName}</strong></td>
        <td>${fmt(item.rentedAt)}</td>
        <td>${item.returnedAt ? fmt(item.returnedAt) : "<span style='color:var(--muted)'>—</span>"}</td>
        <td><span class="badge-status ${item.status}">${item.status === "completed" ? "Hoàn thành" : "Đang thuê"}</span></td>
      </tr>`).join("");
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="4" style="text-align:center;color:var(--red);padding:32px">${err.message}</td></tr>`;
  }
}

// ── Helpers ──────────────────────────────────────────────────
function bindLogout() {
  document.querySelectorAll("#logoutButton").forEach(btn =>
    btn.addEventListener("click", () => {
      localStorage.removeItem(STORAGE_KEYS.token);
      localStorage.removeItem(STORAGE_KEYS.user);
      location.href = "index.html";
    })
  );
}

function setLoading(btn, loading, text) {
  btn.disabled = loading;
  btn.textContent = text;
}

function showAlert(el, msg, type) {
  el.className = `alert alert-${type}`;
  el.textContent = msg;
}

function showModal(el) { el.classList.add("show"); }
function hideModal(el) { el.classList.remove("show"); }

function toast(msg, type = "success") {
  const wrap = document.getElementById("toastWrap");
  if (!wrap) return;
  const el = document.createElement("div");
  el.className = `toast-item ${type}`;
  el.textContent = msg;
  wrap.appendChild(el);
  setTimeout(() => el.remove(), 4000);
}

function fmt(dateStr) {
  return new Date(dateStr).toLocaleString("vi-VN", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit"
  });
}

// ── API Client ───────────────────────────────────────────────
const api = {
  async request(method, path, body) {
    const token = localStorage.getItem(STORAGE_KEYS.token);
    const res = await fetch(API_BASE + path, {
      method,
      headers: {
        "Content-Type": "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
      body: body ? JSON.stringify(body) : undefined
    });

    if (res.status === 401) {
      localStorage.removeItem(STORAGE_KEYS.token);
      location.href = "index.html";
      throw new Error("Phiên đăng nhập hết hạn.");
    }

    const data = res.status !== 204 ? await res.json() : null;
    if (!res.ok) throw new Error(data?.message || "Có lỗi xảy ra.");
    return data;
  },
  get:  (path)       => api.request("GET",  path),
  post: (path, body) => api.request("POST", path, body),
};
