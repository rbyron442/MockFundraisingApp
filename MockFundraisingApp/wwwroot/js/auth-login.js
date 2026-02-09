import { initializeApp } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-app.js";
import {
    getAuth,
    GoogleAuthProvider,
    OAuthProvider,
    signInWithRedirect,
    getRedirectResult,
    signInWithEmailAndPassword,
    createUserWithEmailAndPassword
} from "https://www.gstatic.com/firebasejs/10.12.5/firebase-auth.js";

const firebaseConfig = window.firebaseConfig;
const returnUrl = window.returnUrl || "/";

const app = initializeApp(firebaseConfig);
const auth = getAuth(app);

function showAuthErr(msg) {
    const el = document.getElementById("authErr");
    if (!el) return;
    el.textContent = msg || "";
    el.style.display = msg ? "block" : "none";
}

function friendlyFirebaseError() {
    return "Sign-in failed. Please try again.";
}

async function postIdTokenToServer(user, opts = {}) {
    const idToken = await user.getIdToken();

    const resp = await fetch("/Auth/Session", {
        method: "POST",
        credentials: "same-origin",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            idToken,
            returnUrl,
            accountCreated: !!opts.accountCreated
        })
    });

    if (!resp.ok) throw new Error("Failed to create server session.");
    const data = await resp.json();
    window.location.href = data.returnUrl || "/";
}

// redirect results
try {
    const result = await getRedirectResult(auth);
    if (result?.user) await postIdTokenToServer(result.user);
} catch (e) {
    showAuthErr(friendlyFirebaseError(e));
}

// email login
const form = document.getElementById("emailLoginForm");
if (form) {
    form.addEventListener("submit", async (e) => {
        showAuthErr("");
        if (!form.checkValidity()) return;
        e.preventDefault();

        const email = document.getElementById("Email")?.value?.trim() ?? "";
        const password = document.getElementById("Password")?.value ?? "";

        try {
            const cred = await signInWithEmailAndPassword(auth, email, password);
            await postIdTokenToServer(cred.user);
        } catch (err) {
            showAuthErr(friendlyFirebaseError(err));
        }
    });
}

// signup
document.getElementById("signupEmailBtn")?.addEventListener("click", async () => {
    showAuthErr("");
    if (form && !form.checkValidity()) return;

    const email = document.getElementById("Email")?.value?.trim() ?? "";
    const password = document.getElementById("Password")?.value ?? "";

    try {
        const cred = await createUserWithEmailAndPassword(auth, email, password);
        await postIdTokenToServer(cred.user, { accountCreated: true });
    } catch (err) {
        showAuthErr(friendlyFirebaseError(err));
    }
});

// google/apple buttons (optional)
document.getElementById("googleBtn")?.addEventListener("click", async () => {
    showAuthErr("");
    await signInWithRedirect(auth, new GoogleAuthProvider());
});
document.getElementById("appleBtn")?.addEventListener("click", async () => {
    showAuthErr("");
    await signInWithRedirect(auth, new OAuthProvider("apple.com"));
});