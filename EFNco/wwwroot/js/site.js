// EFNco — site.js

// Auto-dismiss alerts after 5 seconds
document.addEventListener('DOMContentLoaded', function () {
    const alerts = document.querySelectorAll('.alert-success, .alert-warning');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            alert.style.transition = 'opacity 0.4s ease';
            alert.style.opacity = '0';
            setTimeout(function () { alert.remove(); }, 400);
        }, 5000);
    });
});

// ---- Show/Hide Password Toggle ----
document.addEventListener('DOMContentLoaded', function () {
    // Find all password fields and wrap them
    document.querySelectorAll('input[type="password"]').forEach(function (input) {

        // Create wrapper
        var wrapper = document.createElement('div');
        wrapper.style.cssText = 'position:relative;display:block;';

        // Insert wrapper before input and move input inside
        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);

        // Style the input to make room for the icon
        input.style.paddingRight = '42px';

        // Create toggle button
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.setAttribute('tabindex', '-1');
        btn.innerHTML = eyeOffIcon();
        btn.style.cssText = [
            'position:absolute',
            'right:12px',
            'top:50%',
            'transform:translateY(-50%)',
            'background:none',
            'border:none',
            'cursor:pointer',
            'color:var(--gray-500)',
            'padding:0',
            'display:flex',
            'align-items:center',
            'transition:color 0.2s'
        ].join(';');

        btn.addEventListener('mouseenter', function () { btn.style.color = 'var(--gray-300)'; });
        btn.addEventListener('mouseleave', function () { btn.style.color = 'var(--gray-500)'; });

        btn.addEventListener('click', function () {
            var isPassword = input.type === 'password';
            input.type = isPassword ? 'text' : 'password';
            btn.innerHTML = isPassword ? eyeOnIcon() : eyeOffIcon();
        });

        wrapper.appendChild(btn);
    });

    function eyeOffIcon() {
        return '<svg width="18" height="18" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8">' +
            '<path stroke-linecap="round" stroke-linejoin="round" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"/>' +
            '</svg>';
    }

    function eyeOnIcon() {
        return '<svg width="18" height="18" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8">' +
            '<path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>' +
            '<path stroke-linecap="round" stroke-linejoin="round" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>' +
            '</svg>';
    }
});
