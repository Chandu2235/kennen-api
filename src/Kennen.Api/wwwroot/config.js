// Runtime configuration for the static site. Loaded before script.js.
// Kept as a plain script (not a build-time env var) so the API host can be changed
// without a rebuild, and so nothing secret ever lives here.
window.KENNEN_CONFIG = {
  // Base URL of the ASP.NET Core backend, without a trailing slash.
  // Local development points at the API started by `dotnet run`.
  // Replace the production value with your real API host:
  //   - Render: https://kennen-api.onrender.com
  //   - Custom domain: https://api.kennen-technologies.com
  apiBaseUrl: ['localhost', '127.0.0.1'].includes(location.hostname)
    ? 'http://localhost:5220'
    : 'https://kennen-api.onrender.com',

  // Google Analytics 4 measurement ID (Admin > Data streams > Web).
  // Left empty on localhost so local development never pollutes production reports.
  gaMeasurementId: ['localhost', '127.0.0.1'].includes(location.hostname)
    ? ''
    : 'G-QQRTGFGN4V'
};
