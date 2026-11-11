
import api from "@/lib/api";

export type AdmissionHoldStatus =
    | "HELD"
    | "CONFIRMED"
    | "EXPIRED"
    | "CANCELLED";

export interface CreateHoldResponse {
    holdId: string;
    classId: string;
    studentId: string;
    status: AdmissionHoldStatus;
    expiresAt: string;
}

export interface AdmissionHoldResponse {
    holdId: string;
    classId: string;
    status: AdmissionHoldStatus;
    expiresAt: string;
}

export interface ConfirmHoldResponse {
    holdId: string;
    status: AdmissionHoldStatus;
}

export interface StudentHoldResponse {
    holdId: string;
    classId: string;
    className: string;
    status: AdmissionHoldStatus;
    expiresAt: string;
}

// POST /classes/{classId}/holds
export async function createHold(
    classId: string
): Promise<CreateHoldResponse> {
    const { data } = await api.post<CreateHoldResponse>(
        `/classes/${encodeURIComponent(classId)}/holds`
    );

    return data;
}

// GET /holds/{holdId}
export async function getHoldById(
    holdId: string
): Promise<AdmissionHoldResponse> {
    const { data } = await api.get<AdmissionHoldResponse>(
        `/holds/${encodeURIComponent(holdId)}`
    );

    return data;
}

// POST /holds/{holdId}/confirm
export async function confirmHold(
    holdId: string
): Promise<ConfirmHoldResponse> {
    const { data } = await api.post<ConfirmHoldResponse>(
        `/holds/${encodeURIComponent(holdId)}/confirm`
    );

    return data;
}

// DELETE /holds/{holdId}
export async function cancelHold(
    holdId: string
): Promise<void> {
    await api.delete(
        `/holds/${encodeURIComponent(holdId)}`
    );
}

// GET /me/holds
export async function getMyHolds(): Promise<StudentHoldResponse[]> {
    const { data } = await api.get<StudentHoldResponse[]>(
        "/me/holds"
    );

    return data;
}
