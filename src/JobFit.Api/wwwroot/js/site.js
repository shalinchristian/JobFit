// JobFit: tiny progressive enhancements. Everything works without JavaScript.
(function () {
  // Same limits as the server (data-max-bytes / accept come from DocumentTextExtractor).
  function fileProblem(input, f) {
    if (!f) return null;
    var max = parseInt(input.getAttribute('data-max-bytes') || '0', 10);
    if (max && f.size > max) {
      return '"' + f.name + '" is ' + (f.size / 1048576).toFixed(1) + ' MB. Files can be up to ' + Math.round(max / 1048576) + ' MB.';
    }
    var accept = (input.getAttribute('accept') || '').split(',').filter(Boolean);
    var ext = (f.name.match(/\.[^.]+$/) || [''])[0].toLowerCase();
    if (accept.length && accept.indexOf(ext) < 0) {
      return '"' + f.name + '" is not supported. Use ' + accept.join(', ') + '.';
    }
    return null;
  }

  // Resume dropzone: show chosen file name, support drag and drop.
  document.querySelectorAll('[data-dropzone]').forEach(function (zone) {
    var input = zone.querySelector('input[type=file]');
    var label = zone.querySelector('[data-filename]');
    var error = zone.querySelector('[data-file-error]');
    function show() {
      var f = input.files && input.files[0];
      var problem = fileProblem(input, f);
      if (problem) { input.value = ''; f = null; }
      if (error) error.textContent = problem || '';
      label.textContent = f ? f.name : '';
      zone.classList.toggle('has-file', !!f);
      zone.classList.toggle('has-error', !!problem);
    }
    input.addEventListener('change', show);
    ['dragenter', 'dragover'].forEach(function (e) {
      zone.addEventListener(e, function (ev) { ev.preventDefault(); zone.classList.add('drag'); });
    });
    ['dragleave', 'drop'].forEach(function (e) {
      zone.addEventListener(e, function () { zone.classList.remove('drag'); });
    });
    zone.addEventListener('drop', function (ev) {
      ev.preventDefault();
      if (ev.dataTransfer && ev.dataTransfer.files.length) { input.files = ev.dataTransfer.files; show(); }
    });
  });

  // Job posting file: show the name and note that it is used when the text box is empty.
  var jd = document.querySelector('[data-jd-file]');
  if (jd) {
    jd.addEventListener('change', function () {
      var out = document.querySelector('[data-jd-filename]');
      var f = jd.files && jd.files[0];
      var problem = fileProblem(jd, f);
      if (problem) { jd.value = ''; out.textContent = problem; out.classList.add('error'); return; }
      out.classList.remove('error');
      out.textContent = f ? 'Using ' + f.name + ' (pasted text takes priority).' : '';
    });
  }

  // Submit button loading state.
  var form = document.getElementById('check-form');
  if (form) {
    form.addEventListener('submit', function (ev) {
      var bad = Array.prototype.some.call(form.querySelectorAll('input[type=file]'), function (i) {
        return fileProblem(i, i.files && i.files[0]);
      });
      if (bad) { ev.preventDefault(); return; }
      var btn = form.querySelector('[data-submit]');
      if (btn) { btn.classList.add('loading'); btn.querySelector('span').textContent = 'Checking…'; }
    });
  }

  // Status selects submit on change.
  document.querySelectorAll('form[data-autosubmit] select').forEach(function (sel) {
    sel.addEventListener('change', function () { sel.form.submit(); });
  });

  // Confirm deletes.
  document.querySelectorAll('form[data-confirm]').forEach(function (f) {
    f.addEventListener('submit', function (ev) {
      if (!window.confirm(f.getAttribute('data-confirm'))) ev.preventDefault();
    });
  });

  // Auto-hide toast.
  var toast = document.querySelector('.toast');
  if (toast) setTimeout(function () { toast.classList.add('hide'); }, 3500);
})();
