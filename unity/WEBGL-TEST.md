# Get a REAL playable Unity WebGL link

This process builds and publishes **Unity's own WebGL player**, not the previous HTML/CSS/JavaScript game. The Unity project lives in `/unity`. There is no Unity player deployed until the GitHub Actions build succeeds.

1. Open Unity Hub on your desktop and activate a Unity Personal licence under **Preferences > Licenses**. The Unity CI workflow uses the license from that installation. Don't send your Unity credentials in ChatGPT.
2. Open [Memeopoly repository > Settings > Secrets and variables > Actions](https://github.com/willywilburwonka333-gif/Memeopoly/settings/secrets/actions). Create repository secrets `UNITY_LICENSE` (the text of `Unity_lic.ulf`), `UNITY_EMAIL` and `UNITY_PASSWORD`. Paid Unity Pro can instead use `UNITY_SERIAL` with the account credentials.
3. In the repo, open **Settings > Pages** and set **Build and deployment > Source = GitHub Actions**.
4. Open **Actions > Build and publish Memeopoly Unity WebGL > Run workflow**. A green job signifies actual compilation AND publication. An unsuccessful job does not publish a fake Unity build.
5. The action will then show a **page_url**. For this repository, the expected destination (only after the job succeeds) is `https://willywilburwonka333-gif.github.io/Memeopoly/`. Do not mistake this destination for a live build before a successful deploy.
6. The hosted test uses Unity code, not the old web game; your current Vercel `memeopoly.vercel.app` continues to host the original game unchanged.

For Unity Personal CI activation, follow [GameCI's activation guide](https://game.ci/docs/github/activation/). Never put licence data into repository files, commit history or PR comments.

**Requirements:** Unity 6, supported WebGL module, GitHub Actions enabled and enough runner time. The build script is in `unity/Assets/Editor/MemeopolyWebGLBuild.cs`, and the build pipeline is `.github/workflows/unity-webgl.yml`.

**Current limitation:** ChatGPT can commit and audit the project, but this chat does not expose Unity Editor, an activated Unity license or the actual compiled player. Until the workflow finishes successfully, there is no verified playable Unity URL.