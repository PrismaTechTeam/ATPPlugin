// Shared configuration loaded before app.js / standby.js.
// Update `key` if you regenerate the API key in ATPApi/appsettings.json.
window.ATPAPI = {
  base: "http://localhost:5007",
  key:  "atp-pums-f307e5ce067346c19086ba1a8b343779"
};

// Shared toast helper used by both Push handlers. Stack of dismissible toasts
// pinned to the bottom-right of the viewport. Auto-fades after 3.5s.
window.showToast = function (message, success) {
  let $stack = jQuery('#toastStack');
  if (!$stack.length) $stack = jQuery('<div id="toastStack" class="toast-stack"></div>').appendTo(document.body);
  const $t = jQuery('<div class="toast ' + (success ? 'success' : 'error') + '"></div>').text(message);
  $stack.append($t);
  setTimeout(() => $t.fadeOut(200, () => $t.remove()), 3500);
};
