const { app, BrowserWindow } = require("electron");
const path = require("path");
const child_process = require("child_process");

let apiProcess = null;

function resolveApiExe() {
  // When packaged, extraResources are available under process.resourcesPath
  const packagedPath = path.join(
    process.resourcesPath,
    "api",
    "Mobile.Remote.Toolkit.Api.exe"
  );
  if (require("fs").existsSync(packagedPath)) return packagedPath;

  // Development fallback (relative to repo)
  const devPath = path.join(
    __dirname,
    "..",
    "..",
    "Mobile.Remote.Toolkit",
    "publish",
    "Mobile.Remote.Toolkit.Api.exe"
  );
  if (require("fs").existsSync(devPath)) return devPath;

  return null;
}

function startApi() {
  const exe = resolveApiExe();
  if (!exe) {
    console.warn(
      "API executable not found; continuing without starting API. Expected at resources/api/... or Mobile.Remote.Toolkit/publish/..."
    );
    return;
  }

  try {
    apiProcess = child_process.spawn(exe, [], {
      detached: false,
      stdio: "ignore",
    });
    apiProcess.unref && apiProcess.unref();
  } catch (err) {
    console.error("Failed to start API process:", err);
  }
}

function createWindow() {
  const win = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
    },
  });
  // If ELECTRON_DEV is set, load the Vite dev server (fast local iteration)
  if (process.env.ELECTRON_DEV === "1") {
    const devUrl = process.env.ELECTRON_DEV_URL || "http://localhost:5173";
    win.loadURL(devUrl).catch(() => win.loadURL("about:blank"));
    return win;
  }

  // load the built Vue app from app/dist
  const indexHtml = path.join(
    process.resourcesPath,
    "app",
    "dist",
    "index.html"
  );
  if (require("fs").existsSync(indexHtml)) {
    win.loadFile(indexHtml);
  } else {
    // development fallback: load from local path
    const devIndex = path.join(__dirname, "..", "app", "dist", "index.html");
    if (require("fs").existsSync(devIndex)) win.loadFile(devIndex);
    else win.loadURL("about:blank");
  }
}

app.whenReady().then(() => {
  startApi();
  createWindow();
});

app.on("before-quit", () => {
  if (apiProcess) {
    try {
      apiProcess.kill();
    } catch (e) {}
  }
});
