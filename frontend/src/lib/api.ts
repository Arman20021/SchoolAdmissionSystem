
import axios from "axios";

// Backend API URL
// Uses environment variable when provided.
// Falls back to localhost for default development setup.
const API_BASE_URL =
    import.meta.env.VITE_API_BASE_URL?.trim() ||
    "http://localhost:8080";

const api = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
    },
    timeout: 15000,
});

// Automatically attach Student ID
api.interceptors.request.use(
    (config) => {
        const studentId = sessionStorage.getItem(
            "admission.studentId"
        );

        if (studentId) {
            config.headers.set("X-Student-Id", studentId);
        }

        return config;
    },
    (error) => Promise.reject(error)
);

export default api;
