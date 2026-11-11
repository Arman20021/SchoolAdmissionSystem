
import axios from "axios";

const api = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL,
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
