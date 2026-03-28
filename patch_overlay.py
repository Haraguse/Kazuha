import sys
import codecs

try:
    with open(r'h:\Dev\Kazuha\ppt_assistant\ui\overlay.html', 'r', encoding='utf-8') as f:
        content = f.read()

    # 1. CSS
    css_insert = '''        @keyframes marquee-scroll {
            0%, 15% { transform: translateX(0); }
            85%, 100% { transform: translateX(var(--marquee-dist)); }
        }
'''
    if 'marquee-scroll' not in content:
        head_end = content.find('</head>')
        content = content[:head_end] + css_insert + content[head_end:]

    # 2. HTML DOM
    html_old = '''            <div class="status-item" id="status-smtc" style="display: none;">
                <div class="icon-mask icon-music"></div>
                <span class="status-text"></span>
            </div>'''
    html_new = '''            <div class="status-item" id="status-smtc" style="display: none;">
                <div class="icon-mask icon-music" style="flex-shrink:0;"></div>
                <div class="status-marquee-wrapper" style="overflow:hidden; max-width:140px; white-space:nowrap; position:relative; display:flex;">
                    <div class="status-marquee-inner status-text" style="display:inline-block; font-size:14px; font-weight:900; line-height:1;"></div>
                </div>
                <span class="status-text smtc-time-part" style="margin-left: 2px;"></span>
            </div>'''
    content = content.replace(html_old, html_new)

    # 3. hideMusicStatus
    hide_old = '''        function hideMusicStatus() {
            const smtcItem = document.getElementById('status-smtc');
            const smtcText = smtcItem ? smtcItem.querySelector('.status-text') : null;
            if (smtcItem) smtcItem.style.display = 'none';
            if (smtcText) smtcText.textContent = '';
            lastStatusValues.music = '';
            lastStatusValues.musicBase = '';
        }'''
    hide_new = '''        function hideMusicStatus() {
            const smtcItem = document.getElementById('status-smtc');
            const smtcInner = smtcItem ? smtcItem.querySelector('.status-marquee-inner') : null;
            const smtcTime = smtcItem ? smtcItem.querySelector('.smtc-time-part') : null;
            if (smtcItem) smtcItem.style.display = 'none';
            if (smtcInner) {
                smtcInner.textContent = '';
                smtcInner.style.animation = 'none';
            }
            if (smtcTime) smtcTime.textContent = '';
            lastStatusValues.music = '';
            lastStatusValues.musicBase = '';
            lastStatusValues.musicTime = '';
        }'''
    content = content.replace(hide_old, hide_new)

    # 4. renderMusicStatus
    render_start = content.find('        function renderMusicStatus(flipTitle = false) {')
    render_end = content.find('        function updateSystemStatus(data) {', render_start)
    if render_start != -1 and render_end != -1:
        render_new = '''        function renderMusicStatus(flipTitle = false) {
            const showMusic = currentConfig ? (currentConfig.statusBarShowMusic !== false) : true;
            const smtcItem = document.getElementById('status-smtc');
            if (!smtcItem) return;

            if (!showMusic || !currentMusicState.visible) {
                hideMusicStatus();
                return;
            }

            smtcItem.style.display = 'flex';
            
            const baseText = String(currentMusicState.baseText || '').trim();
            if (!baseText) {
                hideMusicStatus();
                return;
            }
            
            const showMusicProgress = currentConfig ? (currentConfig.statusBarShowMusicProgress !== false) : true;
            let timeStr = '';
            let remainingMs = 0;
            let clampedPositionMs = 0;
            
            if (showMusicProgress && currentMusicState.durationMs > 0) {
                const positionMs = getCurrentMusicPositionMs();
                clampedPositionMs = Math.min(positionMs, currentMusicState.durationMs);
                remainingMs = Math.max(currentMusicState.durationMs - clampedPositionMs, 0);
                timeStr = ` ${formatMusicTime(clampedPositionMs / 1000)}/-${formatMusicTime(remainingMs / 1000)}`;
            }

            const smtcInner = smtcItem.querySelector('.status-marquee-inner');
            const smtcTime = smtcItem.querySelector('.smtc-time-part');
            
            if (lastStatusValues.musicBase !== baseText) {
                lastStatusValues.musicBase = baseText;
                
                const updateTitle = () => {
                    smtcInner.textContent = baseText;
                    smtcInner.style.animation = 'none';
                    smtcInner.style.transform = 'translateX(0)';
                    
                    requestAnimationFrame(() => {
                        const wrapper = smtcItem.querySelector('.status-marquee-wrapper');
                        if (!wrapper) return;
                        const scrollW = smtcInner.scrollWidth;
                        const clientW = wrapper.clientWidth; // typically 140px max
                        
                        if (scrollW > clientW) {
                            const dist = clientW - scrollW;
                            const dur = Math.max(3, Math.abs(dist) * 0.04 + 3);  // 40ms per pixel, +3 seconds for pause
                            wrapper.style.setProperty('--marquee-dist', dist + 'px');
                            smtcInner.style.animation = `marquee-scroll ${dur}s linear infinite`;
                        } else {
                            smtcInner.style.animation = 'none';
                            wrapper.style.removeProperty('--marquee-dist');
                        }
                    });
                };
                
                if (flipTitle) {
                    smtcInner.classList.remove('status-music-flip-in');
                    smtcInner.classList.add('status-music-flip');
                    setTimeout(() => {
                        updateTitle();
                        smtcInner.classList.remove('status-music-flip');
                        smtcInner.classList.add('status-music-flip-in');
                        setTimeout(() => smtcInner.classList.remove('status-music-flip-in'), 300);
                    }, 30);
                } else {
                    updateTitle();
                }
            }
            
            if (timeStr && lastStatusValues.musicTime !== timeStr) {
                updateStatusDisplay(smtcTime, timeStr);
                lastStatusValues.musicTime = timeStr;
            } else if (!timeStr) {
                smtcTime.textContent = '';
                lastStatusValues.musicTime = '';
            }
        }
'''
        content = content[:render_start] + render_new + '''
''' + content[render_end:]

    with open(r'h:\Dev\Kazuha\ppt_assistant\ui\overlay.html', 'w', encoding='utf-8') as f:
        f.write(content)
    print('SUCCESS')
except Exception as e:
    print('FAIL', e)
