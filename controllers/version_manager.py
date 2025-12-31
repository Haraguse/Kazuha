import json
import os
import time
import hashlib
import threading
import re
import subprocess
import xml.etree.ElementTree as ET

import requests
from PyQt6.QtCore import QObject, pyqtSignal, QCoreApplication

def tr(text: str) -> str:
    return QCoreApplication.translate("VersionManager", text)

class VersionManager(QObject):
    update_available = pyqtSignal(dict)
    update_progress = pyqtSignal(int)
    update_error = pyqtSignal(str)
    update_complete = pyqtSignal()
    update_check_finished = pyqtSignal()

    def __init__(self, config_path="config/version.json", repo_owner="Haraguse", repo_name="Kazuha"):
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
                data = json.load(f)
                return data
        except Exception as e:
            print(f"Error reading version info: {e}")
            return None

    def save_version_info(self, info):
        try:
            temp_path = self.config_path + ".tmp"
            with open(temp_path, 'w', encoding='utf-8') as f:
                json.dump(info, f, indent=4)
                f.flush()
                os.fsync(f.fileno())
            
            if os.path.exists(self.config_path):
                os.remove(self.config_path)
            os.rename(temp_path, self.config_path)
            
        except Exception as e:
            print(f"Error saving version info: {e}")

    def check_for_updates(self):
        threading.Thread(target=self._check_for_updates_thread, daemon=True).start()

    def _check_for_updates_thread(self):
        try:
            url = f"https://api.github.com/repos/{self.repo_owner}/{self.repo_name}/releases/latest"
            headers = {'Accept': 'application/vnd.github.v3+json'}
            
            max_retries = 2
            for attempt in range(max_retries):
                try:
                    response = requests.get(url, headers=headers, timeout=10)
                    if response.status_code == 200:
                        self._handle_release_data(response.json())
                        return
                except Exception:
                    pass
                time.sleep(1)

            mirrors = ["api.bgithub.xyz", "api.github-api.com"]
            for mirror in mirrors:
                try:
                    mirror_url = url.replace("api.github.com", mirror)
                    response = requests.get(mirror_url, headers=headers, timeout=10)
                    if response.status_code == 200:
                        self._handle_release_data(response.json())
                        return
                except Exception:
                    continue

            self._check_updates_via_feed()
        finally:
            try:
                self.update_check_finished.emit()
            except Exception:
                pass

    def _handle_release_data(self, release_data):
        try:
            name = release_data.get('name', '')
            tag_name = release_data.get('tag_name', '')
            
            remote_code = None
            match = re.search(r'（(\d+)）', name) or re.search(r'（(\d+)）', tag_name)
            if match:
                remote_code = int(match.group(1))
            else:
                match = re.search(r'\((\d+)\)', name) or re.search(r'\((\d+)\)', tag_name)
                if match:
                    remote_code = int(match.group(1))
            
            local_version_info = self.current_version_info or {}
            local_code = int(local_version_info.get('versionCode', '0'))
            
            if remote_code is not None and remote_code > local_code:
                self.latest_release_info = release_data
                self.update_available.emit({
                    'version': release_data.get('tag_name', 'v0.0.0'),
                    'versionCode': remote_code,
                    'name': name,
                    'body': release_data.get('body', ''),
                    'assets': release_data.get('assets', [])
                })
            else:
                print(f"No new version. Remote: {remote_code}, Local: {local_code}")
        except Exception as e:
            print(f"Handle release data error: {e}")

    def _check_updates_via_feed(self):
        try:
            feed_url = f"https://github.com/Haraguse/Kazuha/releases.atom"
            sources = [
                feed_url,
                feed_url.replace("github.com", "bgithub.xyz"),
                feed_url.replace("github.com", "kkgithub.com")
            ]
            
            r = None
            for src in sources:
                try:
                    r = requests.get(src, timeout=10)
                    if r.status_code == 200:
                        break
                except Exception:
                    continue
            
            if not r or r.status_code != 200:
                return

            root = ET.fromstring(r.text)
            ns = {'atom': 'http://www.w3.org/2005/Atom'}
            entry = root.find('atom:entry', ns)
            if entry is None:
                return
                
            title_el = entry.find('atom:title', ns)
            content_el = entry.find('atom:content', ns)
            tag_title = title_el.text.strip() if title_el is not None and title_el.text else ''
            
            body = content_el.text or '' if content_el is not None else ''
            
            body = re.sub(r'<br\s*/?>', '\n', body)
            body = re.sub(r'<[^>]+>', '', body)
            
            self._handle_release_data({
                'tag_name': tag_title.split()[0] if tag_title else 'v0.0.0',
                'name': tag_title,
                'body': body,
                'assets': []
            })
        except Exception:
            pass

    def download_and_install(self, asset_url, sha256_hash=None):
        threading.Thread(target=self._download_and_install_thread, args=(asset_url, sha256_hash), daemon=True).start()

    def _download_and_install_thread(self, asset_url, sha256_hash=None):
        try:
            urls = [asset_url]
            if "github.com" in asset_url:
                urls.append(f"https://ghproxy.net/{asset_url}")
                urls.append(asset_url.replace("github.com", "kkgithub.com"))
            
            response = None
            last_err = None
            for url in urls:
                try:
                    response = requests.get(url, stream=True, timeout=20)
                    if response.status_code == 200:
                        break
                except Exception as e:
                    last_err = e
                    continue
            
            if not response or response.status_code != 200:
                self.update_error.emit(tr("无法连接到下载服务器: {0}").format(str(last_err or "Unknown")))
                return

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
                    self.update_error.emit(tr("文件哈希校验失败"))
                    return
                else:
                    print("Hash verification passed.")

            self.install_update(temp_file)
            return True
            
        except Exception as e:
            self.update_error.emit(tr("下载或安装更新时出错: {e}").format(e=str(e)))
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
        try:
            subprocess.Popen([installer_path, "/S"], shell=True)
            self.update_complete.emit()
        except Exception as e:
            self.update_error.emit(tr("安装失败: {e}").format(e=e))
