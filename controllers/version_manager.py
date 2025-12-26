import json
import os
import time
import requests
import hashlib
import msvcrt
from packaging import version
from PyQt6.QtCore import QObject, pyqtSignal

class VersionManager(QObject):
    update_available = pyqtSignal(dict)
    update_progress = pyqtSignal(int)
    update_error = pyqtSignal(str)
    update_complete = pyqtSignal()
    update_check_finished = pyqtSignal()

    def __init__(self, config_path="config/version.json", repo_owner="owner", repo_name="repo"):
        super().__init__()
        self.config_path = os.path.abspath(config_path)
        self.repo_owner = repo_owner
        self.repo_name = repo_name
        self.current_version_info = self.load_version_info()
        self.latest_release_info = None

    def load_version_info(self):
        if not os.path.exists(self.config_path):
            return None
        
        try:
            with open(self.config_path, 'r', encoding='utf-8') as f:
                # Removed msvcrt locking for simplicity and stability
                data = json.load(f)
                return data
        except Exception as e:
            print(f"Error reading version info: {e}")
            return None

    def save_version_info(self, info):
        try:
            # Atomic write: write to temp file then rename
            temp_path = self.config_path + ".tmp"
            with open(temp_path, 'w', encoding='utf-8') as f:
                json.dump(info, f, indent=4)
                f.flush()
                os.fsync(f.fileno())
            
            # Atomic replace
            if os.path.exists(self.config_path):
                os.remove(self.config_path)
            os.rename(temp_path, self.config_path)
            
        except Exception as e:
            print(f"Error saving version info: {e}")

    def check_for_updates(self):
        import threading
        threading.Thread(target=self._check_for_updates_thread, daemon=True).start()

    def _check_for_updates_thread(self):
        try:
            url = f"https://api.github.com/repos/{self.repo_owner}/{self.repo_name}/releases/latest"
            headers = {'Accept': 'application/vnd.github.v3+json'}
            max_retries = 3
            backoff_factor = 2
            for attempt in range(max_retries):
                try:
                    response = requests.get(url, headers=headers, timeout=10)
                    if response.status_code == 200:
                        release_data = response.json()
                        self._handle_release_data(release_data)
                        return
                    elif response.status_code == 403:
                        print(f"Rate limit exceeded. Retrying in {backoff_factor ** attempt}s...")
                    else:
                        print(f"Failed to check updates: {response.status_code}")
                except requests.RequestException as e:
                    print(f"Network error: {e}")
                except Exception as e:
                    print(f"Unexpected error in update check: {e}")
                time.sleep(backoff_factor ** attempt)
            try:
                mirror_url = url.replace("api.github.com", "api.bgithub.xyz")
                response = requests.get(mirror_url, headers=headers, timeout=10)
                if response.status_code == 200:
                    release_data = response.json()
                    self._handle_release_data(release_data)
                    return
                else:
                    print(f"Failed to check updates via mirror: {response.status_code}")
            except Exception as e:
                print(f"Unexpected error in mirror update check: {e}")
            self._check_updates_via_feed()
        finally:
            try:
                self.update_check_finished.emit()
            except Exception:
                pass

    def _handle_release_data(self, release_data):
        remote_version_str = release_data.get('tag_name', '0.0.0').lstrip('v')
        local_version_str = self.current_version_info.get('versionName', '0.0.0')
        remote_version_str = remote_version_str.replace('α', 'a').replace('β', 'b')
        local_version_str = local_version_str.replace('α', 'a').replace('β', 'b')
        def normalize(v):
            v = v.strip()
            if v.startswith('a') and len(v) > 1:
                core = v[1:]
                parts = core.split('.')
                if len(parts) >= 2 and all(p.isdigit() for p in parts):
                    if len(parts) == 2:
                        core = core + '.0'
                    return core + 'a0'
            return v
        remote_version_str = normalize(remote_version_str)
        local_version_str = normalize(local_version_str)
        try:
            remote_ver = version.parse(remote_version_str)
            local_ver = version.parse(local_version_str)
            if remote_ver > local_ver:
                self.latest_release_info = release_data
                self.update_available.emit({
                    'version': remote_version_str,
                    'name': release_data.get('name', ''),
                    'body': release_data.get('body', ''),
                    'assets': release_data.get('assets', [])
                })
            else:
                print("No new version found.")
        except Exception as e:
            print(f"Version parsing error: {e}")

    def _check_updates_via_feed(self):
        try:
            feed_url = f"https://github.com/{self.repo_owner}/{self.repo_name}/releases.atom"
            r = requests.get(feed_url, timeout=10)
            if r.status_code != 200:
                mirror_feed_url = feed_url.replace("github.com", "bgithub.xyz")
                r = requests.get(mirror_feed_url, timeout=10)
                if r.status_code != 200:
                    print(f"Failed to fetch releases feed: {r.status_code}")
                    return
            import xml.etree.ElementTree as ET
            root = ET.fromstring(r.text)
            ns = {'atom': 'http://www.w3.org/2005/Atom'}
            entry = root.find('atom:entry', ns)
            if entry is None:
                print("No entries found in releases feed.")
                return
            title_el = entry.find('atom:title', ns)
            content_el = entry.find('atom:content', ns)
            tag_title = title_el.text.strip() if title_el is not None and title_el.text else ''
            remote_version_str = tag_title.lstrip('v').split()[0] if tag_title else '0.0.0'
            body = content_el.text or '' if content_el is not None else ''
            import re
            body = re.sub(r'<br\s*/?>', '\n', body)
            body = re.sub(r'<[^>]+>', '', body)
            release_data = {
                'tag_name': remote_version_str,
                'name': tag_title,
                'body': body,
                'assets': []
            }
            self._handle_release_data(release_data)
        except Exception as e:
            print(f"Failed to check updates via feed: {e}")

    def download_and_install(self, asset_url, sha256_hash=None):
        import threading
        threading.Thread(target=self._download_and_install_thread, args=(asset_url, sha256_hash), daemon=True).start()

    def _download_and_install_thread(self, asset_url, sha256_hash=None):
        try:
            response = requests.get(asset_url, stream=True)
            total_size = int(response.headers.get('content-length', 0))
            downloaded_size = 0
            
            temp_file = os.path.join(os.environ.get('TEMP', '.'), "update_installer.exe")
            
            with open(temp_file, 'wb') as f:
                for chunk in response.iter_content(chunk_size=8192):
                    if chunk:
                        f.write(chunk)
                        downloaded_size += len(chunk)
                        if total_size > 0:
                            progress = int((downloaded_size / total_size) * 100)
                            self.update_progress.emit(progress)
            
            if sha256_hash:
                if not self.verify_hash(temp_file, sha256_hash):
                    self.update_error.emit("Hash verification failed")
                    return
                else:
                    print("Hash verification passed.")

            self.install_update(temp_file)
            return True
            
        except Exception as e:
            self.update_error.emit(str(e))
            return False

    def verify_hash(self, file_path, expected_hash):
        if not expected_hash:
            return True
        sha256_hash = hashlib.sha256()
        with open(file_path, "rb") as f:
            for byte_block in iter(lambda: f.read(4096), b""):
                sha256_hash.update(byte_block)
        return sha256_hash.hexdigest().lower() == expected_hash.lower()

    def install_update(self, installer_path):
        # Silent install
        import subprocess
        try:
            # Assuming Inno Setup or similar with /S or /VERYSILENT
            # User requirement: "Silent installation mode"
            subprocess.Popen([installer_path, "/S"], shell=True)
            self.update_complete.emit()
        except Exception as e:
            self.update_error.emit(f"Installation failed: {e}")
