# Clean Browser delivery workflow

- Preserve the native Windows Forms/WebView2 project, approved colors/artwork and user data in %LOCALAPPDATA%\KayBrowser.
- Deliver updates through the application's GitHub Releases updater. Do not hand the user ZIPs or EXEs as the normal delivery or require reinstalling.
- Bump the app version, update release notes, build, run isolated logic/native UI tests, create a PR, wait for passing checks, merge and wait for the release workflow and update smoke test. Publish only builds that compile and open.
- Keep CleanBrowser.exe, its SHA-256 and legacy KayBrowser.exe aliases compatible with installed updaters.
- Do not claim live AI-provider validation without actual credentials. Use mocked HTTP and synthetic windows for diagnostics; never capture unrelated user windows or personal screens in tests.
- AI captures must be explicitly selected, previewed and sent by the user; no continuous background capture. Keep provider keys protected and bound to their endpoint.
