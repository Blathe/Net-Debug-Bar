(function() {
    const tabs = document.querySelectorAll('#ndb-bar .ndb-tab');
    const contents = document.querySelectorAll('#ndb-bar .ndb-tab-content');
    const toggle = document.getElementById('ndb-toggle');
    const bar = document.getElementById('ndb-bar');

    // Tab switching
    tabs.forEach(tab => {
        tab.addEventListener('click', () => {
            const target = tab.dataset.tab;
            if (!target) return;

            // Activate clicked tab
            tabs.forEach(t => t.classList.remove('active'));
            tab.classList.add('active');

            // Show corresponding content
            contents.forEach(c => c.classList.remove('active'));
            const targetContent = document.getElementById('ndb-tab-' + target);
            if (targetContent) {
                targetContent.classList.add('active');
            }
        });
    });

    // Toggle collapse/expand
    if (toggle && bar) {
        toggle.addEventListener('click', () => {
            bar.classList.toggle('collapsed');
            toggle.textContent = bar.classList.contains('collapsed') ? '+' : '−';
        });
    }

    // Log level filtering
    document.querySelectorAll('.ndb-log-filter').forEach(btn => {
        btn.addEventListener('click', function() {
            this.classList.toggle('active');
            const level = this.dataset.level;
            const display = this.classList.contains('active') ? '' : 'none';
            document.querySelectorAll('.ndb-log-entry[data-level="' + level + '"]').forEach(entry => {
                entry.style.display = display;
            });
        });
    });
})();
