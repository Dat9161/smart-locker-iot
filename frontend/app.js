const STORAGE_KEYS = {
  token: "smartLockerToken",
  user: "smartLockerUser",
  lockers: "smartLockerLockers",
  history: "smartLockerHistory"
};

const API_CONFIG = {
  baseUrl: "http://localhost:5000",
  endpoints: {
    login: "/api/auth/login",
    lockers: "/api/lockers",
    rentLocker: "/api/lockers/rent",
    history: "/api/history"
  }
};

const INITIAL_LOCKERS = [
  { id: 1, name: "Tủ 01", status: "available", location: "Khu A - Tầng 1" },
  { id: 2, name: "Tủ 02", status: "occupied", location: "Khu A - Tầng 1" },
  { id: 3, name: "Tủ 03", status: "available", location: "Khu A - Tầng 1" },
  { id: 4, name: "Tủ 04", status: "available", location: "Khu A - Tầng 2" },
  { id: 5, name: "Tủ 05", status: "occupied", location: "Khu B - Tầng 1" },
  { id: 6, name: "Tủ 06", status: "available", location: "Khu B - Tầng 1" },
  { id: 7, name: "Tủ 07", status: "available", location: "Khu B - Tầng 2" },
  { id: 8, name: "Tủ 08", status: "occupied", location: "Khu C - Tầng 1" }
];

const INITIAL_HISTORY = [
  { renterName: "Nguyen Van A", lockerNumber: "Tủ 02", rentedAt: "08:15" },
  { renterName: "Tran Thi B", lockerNumber: "Tủ 05", rentedAt: "09:40" },
  { renterName: "Le Minh C", lockerNumber: "Tủ 08", rentedAt: "10:05" }
];

document.addEventListener("DOMContentLoaded", () => {
  initializeMockStorage();
  bindLogoutButtons();

  const pageName = getCurrentPageName();

  if (pageName === "index.html" || pageName === "") {
    setupLoginPage();
    return;
  }

  const isAuthorized = protectPrivatePage();
  if (!isAuthorized) {
    return;
  }

  if (pageName === "dashboard.html") {
    setupDashboardPage();
  }

  if (pageName === "history.html") {
    setupHistoryPage();
  }
});

function getCurrentPageName() {
  const path = window.location.pathname.split("/").pop();
  return path.toLowerCase();
}

function initializeMockStorage() {
  if (!localStorage.getItem(STORAGE_KEYS.lockers)) {
    localStorage.setItem(STORAGE_KEYS.lockers, JSON.stringify(INITIAL_LOCKERS));
  }

  if (!localStorage.getItem(STORAGE_KEYS.history)) {
    localStorage.setItem(STORAGE_KEYS.history, JSON.stringify(INITIAL_HISTORY));
  }
}

function bindLogoutButtons() {
  const logoutButtons = document.querySelectorAll("#logoutButton");

  logoutButtons.forEach((button) => {
    button.addEventListener("click", () => {
      localStorage.removeItem(STORAGE_KEYS.token);
      localStorage.removeItem(STORAGE_KEYS.user);
      sessionStorage.removeItem(STORAGE_KEYS.token);
      sessionStorage.removeItem(STORAGE_KEYS.user);
      window.location.href = "index.html";
    });
  });
}

function protectPrivatePage() {
  const token = localStorage.getItem(STORAGE_KEYS.token);

  if (!token) {
    window.location.href = "index.html";
    return false;
  }

  return true;
}

function setupLoginPage() {
  const token = localStorage.getItem(STORAGE_KEYS.token);
  if (token) {
    window.location.href = "dashboard.html";
    return;
  }

  const loginForm = document.getElementById("loginForm");
  const loginButton = document.getElementById("loginButton");
  const loginAlert = document.getElementById("loginAlert");

  if (!loginForm || !loginButton || !loginAlert) {
    return;
  }

  loginForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    const username = document.getElementById("username").value.trim();
    const password = document.getElementById("password").value.trim();

    if (!username || !password) {
      showAlert(loginAlert, "Vui lòng nhập đầy đủ username và password.", "danger");
      return;
    }

    loginButton.disabled = true;
    loginButton.textContent = "Đang đăng nhập...";

    try {
      const response = await authApi.login({ username, password });

      localStorage.setItem(STORAGE_KEYS.token, response.token);
      localStorage.setItem(STORAGE_KEYS.user, JSON.stringify(response.user));

      showAlert(loginAlert, "Đăng nhập thành công. Đang chuyển hướng...", "success");

      setTimeout(() => {
        window.location.href = "dashboard.html";
      }, 700);
    } catch (error) {
      showAlert(loginAlert, error.message || "Đăng nhập thất bại.", "danger");
    } finally {
      loginButton.disabled = false;
      loginButton.textContent = "Đăng nhập";
    }
  });
}

async function setupDashboardPage() {
  const lockerGrid = document.getElementById("lockerGrid");
  const totalLockers = document.getElementById("totalLockers");
  const availableLockers = document.getElementById("availableLockers");
  const occupiedLockers = document.getElementById("occupiedLockers");
  const selectedLockerLabel = document.getElementById("selectedLockerLabel");
  const confirmRentButton = document.getElementById("confirmRentButton");
  const modalElement = document.getElementById("rentLockerModal");
  const toastElement = document.getElementById("appToast");

  if (!lockerGrid || !confirmRentButton || !modalElement || !toastElement) {
    return;
  }

  const rentModal = new bootstrap.Modal(modalElement);
  const appToast = new bootstrap.Toast(toastElement, { delay: 4200 });
  let selectedLockerId = null;
  let lockers = await lockerApi.getLockers();

  const openRentModal = (lockerId) => {
    const locker = lockers.find((item) => item.id === lockerId);
    if (!locker) {
      return;
    }

    selectedLockerId = locker.id;
    selectedLockerLabel.textContent = locker.name;
    rentModal.show();
  };

  renderLockerGrid(lockers, lockerGrid, openRentModal);
  updateLockerStats(lockers, { totalLockers, availableLockers, occupiedLockers });

  confirmRentButton.addEventListener("click", async () => {
    if (!selectedLockerId) {
      return;
    }

    confirmRentButton.disabled = true;
    confirmRentButton.textContent = "Đang xử lý...";

    try {
      /*
        Luồng xử lý thuê tủ:
        1. Người dùng chọn một tủ đang trống trên giao diện.
        2. Khi xác nhận trong popup, frontend gọi API POST /api/lockers/rent.
        3. Mock API cập nhật trạng thái tủ sang "occupied" và ghi lịch sử thuê.
        4. Ngay sau khi API thành công, frontend cập nhật dữ liệu local, đổi màu ô tủ,
           cập nhật lại bộ đếm và hiện toast thành công mà không cần tải lại trang.
      */
      const result = await lockerApi.rentLocker({ lockerId: selectedLockerId });
      lockers = lockers.map((locker) => {
        if (locker.id === selectedLockerId) {
          return { ...locker, status: "occupied" };
        }

        return locker;
      });

      rentModal.hide();
      renderLockerGrid(lockers, lockerGrid, openRentModal);
      updateLockerStats(lockers, { totalLockers, availableLockers, occupiedLockers });
      showToast(
        appToast,
        "Thuê tủ thành công! Tủ đang mở, vui lòng ra tủ.",
        "success"
      );
      selectedLockerId = null;
    } catch (error) {
      showToast(appToast, error.message || "Không thể thuê tủ.", "danger");
    } finally {
      confirmRentButton.disabled = false;
      confirmRentButton.textContent = "Thuê tủ";
    }
  });
}

async function setupHistoryPage() {
  const historyTableBody = document.getElementById("historyTableBody");
  const historyCount = document.getElementById("historyCount");

  if (!historyTableBody || !historyCount) {
    return;
  }

  const historyItems = await historyApi.getHistory();

  historyTableBody.innerHTML = historyItems.map((item) => `
    <tr>
      <td>${item.renterName}</td>
      <td>${item.lockerNumber}</td>
      <td>${item.rentedAt}</td>
    </tr>
  `).join("");

  historyCount.textContent = `${historyItems.length} bản ghi`;
}

function renderLockerGrid(lockers, container, onRentClick) {
  container.innerHTML = lockers.map((locker) => {
    const isAvailable = locker.status === "available";
    const statusText = isAvailable ? "Tủ đang trống" : "Đang có người dùng";

    return `
      <button
        class="locker-card ${isAvailable ? "available" : "occupied"}"
        data-locker-id="${locker.id}"
        ${isAvailable ? "" : "disabled"}
      >
        <span class="locker-number">${locker.name}</span>
        <span class="locker-status">${statusText}</span>
        <div class="locker-meta">${locker.location}</div>
      </button>
    `;
  }).join("");

  container.querySelectorAll(".locker-card.available").forEach((button) => {
    button.addEventListener("click", () => {
      const lockerId = Number(button.dataset.lockerId);
      onRentClick(lockerId);
    });
  });
}

function updateLockerStats(lockers, targets) {
  const availableCount = lockers.filter((locker) => locker.status === "available").length;
  const occupiedCount = lockers.length - availableCount;

  targets.totalLockers.textContent = lockers.length;
  targets.availableLockers.textContent = availableCount;
  targets.occupiedLockers.textContent = occupiedCount;
}

function showAlert(element, message, type) {
  element.className = `alert alert-${type} mt-4 mb-0`;
  element.textContent = message;
}

function showToast(toastInstance, message, type) {
  const toastElement = document.getElementById("appToast");
  const toastMessage = document.getElementById("toastMessage");

  toastElement.classList.remove("toast-error");
  if (type === "danger") {
    toastElement.classList.add("toast-error");
  }
  toastMessage.textContent = message;
  toastInstance.show();
}

function getStoredLockers() {
  return JSON.parse(localStorage.getItem(STORAGE_KEYS.lockers) || "[]");
}

function setStoredLockers(lockers) {
  localStorage.setItem(STORAGE_KEYS.lockers, JSON.stringify(lockers));
}

function getStoredHistory() {
  return JSON.parse(localStorage.getItem(STORAGE_KEYS.history) || "[]");
}

function setStoredHistory(history) {
  localStorage.setItem(STORAGE_KEYS.history, JSON.stringify(history));
}

function formatTime(date = new Date()) {
  return date.toLocaleTimeString("vi-VN", {
    hour: "2-digit",
    minute: "2-digit"
  });
}

function delay(ms = 350) {
  return new Promise((resolve) => {
    window.setTimeout(resolve, ms);
  });
}

/*
  Các hàm fetch mock được đóng gói thành object API riêng.
  Khi backend thật sẵn sàng, chỉ cần thay phần mock bằng fetch(API_CONFIG.baseUrl + endpoint).
*/
const authApi = {
  async login(credentials) {
    await delay();

    if (credentials.username === "student01" && credentials.password === "123456") {
      return {
        token: "mock-jwt-token-smart-locker",
        user: {
          username: credentials.username,
          fullName: "Sinh vien demo"
        }
      };
    }

    throw new Error("Sai username hoặc password.");
  }
};

const lockerApi = {
  async getLockers() {
    await delay(200);
    return getStoredLockers();
  },

  async rentLocker({ lockerId }) {
    await delay(500);

    const lockers = getStoredLockers();
    const lockerIndex = lockers.findIndex((locker) => locker.id === lockerId);

    if (lockerIndex === -1) {
      throw new Error("Không tìm thấy tủ cần thuê.");
    }

    if (lockers[lockerIndex].status !== "available") {
      throw new Error("Tủ này hiện không còn trống.");
    }

    lockers[lockerIndex].status = "occupied";
    setStoredLockers(lockers);

    const currentUser = JSON.parse(localStorage.getItem(STORAGE_KEYS.user) || "{}");
    const history = getStoredHistory();

    history.unshift({
      renterName: currentUser.fullName || currentUser.username || "Người dùng",
      lockerNumber: lockers[lockerIndex].name,
      rentedAt: formatTime()
    });
    setStoredHistory(history);

    /*
      Request giả lập tương ứng backend thật sau này:
      POST /api/lockers/rent
      Body: { lockerId: ID_cua_tu }
    */
    return {
      endpoint: `${API_CONFIG.baseUrl}${API_CONFIG.endpoints.rentLocker}`,
      method: "POST",
      message: "Tủ đang mở, vui lòng ra tủ."
    };
  }
};

const historyApi = {
  async getHistory() {
    await delay(180);
    return getStoredHistory();
  }
};
