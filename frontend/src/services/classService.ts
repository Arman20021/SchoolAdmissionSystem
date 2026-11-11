
import api from "@/lib/api";

export interface ClassResponse {
    id: string;
    name: string;
    totalSeats: number;
    heldSeats: number;
    confirmedSeats: number;
    availableSeats: number;
}

export interface CreateClassRequest {
    name: string;
    totalSeats: number;
}

// POST /classes
export async function createClass(
    request: CreateClassRequest
): Promise<ClassResponse> {
    const { data } = await api.post<ClassResponse>(
        "/classes",
        request
    );

    return data;
}

// GET /classes
export async function getClasses(): Promise<ClassResponse[]> {
    const { data } = await api.get<ClassResponse[]>("/classes");

    return data;
}

// GET /classes/{classId}
export async function getClassById(
    classId: string
): Promise<ClassResponse> {
    const { data } = await api.get<ClassResponse>(
        `/classes/${encodeURIComponent(classId)}`
    );

    return data;
}
