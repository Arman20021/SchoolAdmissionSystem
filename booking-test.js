
import http from "k6/http";
import { check } from "k6";

const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";
const CLASS_ID = __ENV.CLASS_ID;
const RUN_PREFIX = __ENV.RUN_PREFIX || "876543210";
const VUS = Number(__ENV.VUS || 50);

if (!/^\d{9}$/.test(RUN_PREFIX)) {
    throw new Error("RUN_PREFIX must contain exactly 9 digits.");
}

export const options = {
    scenarios: {
        simultaneous_booking: {
            executor: "per-vu-iterations",
            vus: VUS,
            iterations: 1,
            maxDuration: "60s",
        },
    },
};

export default function () {

    if (!CLASS_ID) {
        throw new Error("Please provide CLASS_ID.");
    }

    // Generate a unique 17-digit student ID for every user
    const studentId =
        RUN_PREFIX +
        String(__VU).padStart(4, "0") +
        String(__ITER).padStart(4, "0");

    // Send admission application
    const response = http.post(
        `${BASE_URL}/classes/${encodeURIComponent(CLASS_ID)}/holds`,
        null,
        {
            headers: {
                "X-Student-Id": studentId,
                "Accept": "application/json",
            },
        }
    );

    // Only 201 and 409 are expected
    const validResponse = check(response, {
        "Request returns 201 or 409": (r) =>
            r.status === 201 || r.status === 409,
    });

    if (!validResponse) {
        console.error(
            `Student ${studentId}: HTTP ${response.status} - ${response.body}`
        );
    }

    if (response.status === 201) {
        console.log(`SUCCESS: Student ${studentId}`);
    }

    if (response.status === 409) {
        console.log(`NO SEAT / CONFLICT: Student ${studentId}`);
    }
}
