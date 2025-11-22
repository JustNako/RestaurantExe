// Converts elements with `data-datetime` ISO timestamps to user's local time
document.addEventListener('DOMContentLoaded', function () {
  try {
    document.querySelectorAll('[data-datetime]').forEach(function (el) {
      var v = el.getAttribute('data-datetime');
      if (!v) return;
      var d = new Date(v);
      if (isNaN(d.getTime())) return;
      var fmt = el.getAttribute('data-format') || '';
      var out = '';
      if (fmt === 'date') {
        out = d.toLocaleDateString();
      } else if (fmt === 'time') {
        out = d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      } else if (fmt === 'short') {
        out = d.toLocaleString([], { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' });
      } else {
        out = d.toLocaleString();
      }
      el.textContent = out;
    });
  } catch (e) {
    console && console.error && console.error('localtime.js error', e);
  }
});
