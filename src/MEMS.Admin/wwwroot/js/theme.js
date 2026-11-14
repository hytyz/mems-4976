(function () {
  var root = document.documentElement;
  try {
    var saved = localStorage.getItem('mems-theme');
    if (saved === 'dark' || saved === 'light') {
      root.setAttribute('data-bs-theme', saved);
    } else if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
      root.setAttribute('data-bs-theme', 'dark');
    }
  } catch (e) { }

  document.addEventListener('click', function (event) {
    var button = event.target.closest ? event.target.closest('[data-theme-toggle]') : null;
    if (!button) {
      return;
    }
    var next = root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
    root.setAttribute('data-bs-theme', next);
    try { localStorage.setItem('mems-theme', next); } catch (e) { }
  });
})();
