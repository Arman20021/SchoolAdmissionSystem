
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import App from "./App";
import "./index.css";

import { StudentIdentityProvider } from "./features/student/StudentIdentityContext";

createRoot(document.getElementById("root")!).render(
    <StrictMode>
        <StudentIdentityProvider>
            <App />
        </StudentIdentityProvider>
    </StrictMode>
);
