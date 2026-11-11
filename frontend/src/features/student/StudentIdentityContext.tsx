
import {
    createContext,
    useContext,
    useState,
    type ReactNode,
} from "react";

interface StudentIdentityContextType {
    studentId: string | null;
    setStudentId: (id: string) => boolean;
    clearStudentId: () => void;
}

const STORAGE_KEY = "admission.studentId";

const StudentIdentityContext =
    createContext<StudentIdentityContextType | undefined>(
        undefined
    );

export function StudentIdentityProvider({
    children,
}: {
    children: ReactNode;
}) {
    const [studentId, setStudentIdState] = useState<string | null>(
        () => {
            const savedId = sessionStorage.getItem(STORAGE_KEY);

            return savedId && /^\d{17}$/.test(savedId)
                ? savedId
                : null;
        }
    );

    const setStudentId = (id: string): boolean => {
        const normalizedId = id.trim();

        if (!/^\d{17}$/.test(normalizedId)) {
            return false;
        }

        sessionStorage.setItem(STORAGE_KEY, normalizedId);
        setStudentIdState(normalizedId);

        return true;
    };

    const clearStudentId = () => {
        sessionStorage.removeItem(STORAGE_KEY);
        setStudentIdState(null);
    };

    return (
        <StudentIdentityContext.Provider
            value={{
                studentId,
                setStudentId,
                clearStudentId,
            }}
        >
            {children}
        </StudentIdentityContext.Provider>
    );
}

export function useStudentIdentity() {
    const context = useContext(StudentIdentityContext);

    if (!context) {
        throw new Error(
            "useStudentIdentity must be used inside StudentIdentityProvider."
        );
    }

    return context;
}
