
import { useState, type FormEvent } from "react";
import { useStudentIdentity } from "@/features/student/StudentIdentityContext";

interface StudentIdentityFormProps {
    onSuccess?: () => void;
    onCancel?: () => void;
}

export default function StudentIdentityForm({
    onSuccess,
    onCancel,
}: StudentIdentityFormProps) {
    const { setStudentId } = useStudentIdentity();

    const [value, setValue] = useState("");
    const [error, setError] = useState("");

    const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        if (!/^\d{17}$/.test(value)) {
            setError("Student ID must contain exactly 17 digits.");
            return;
        }

        const success = setStudentId(value);

        if (success) {
            setError("");
            onSuccess?.();
        }
    };

    return (
        <div className="mx-auto w-full max-w-md rounded-xl border border-gray-200 bg-white p-8 shadow-sm">
            <div className="mb-7">
                <h2 className="text-xl font-semibold tracking-tight">
                    Student Identification
                </h2>

                <p className="mt-2 text-sm text-gray-500">
                    Enter your 17-digit Student ID to continue.
                </p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
                <div>
                    <label
                        htmlFor="studentId"
                        className="mb-2 block text-sm font-medium"
                    >
                        Student ID
                    </label>

                    <input
                        id="studentId"
                        type="text"
                        inputMode="numeric"
                        autoComplete="off"
                        maxLength={17}
                        value={value}
                        onChange={(event) => {
                            setValue(
                                event.target.value.replace(/\D/g, "").slice(0, 17)
                            );
                            setError("");
                        }}
                        placeholder="Enter 17-digit Student ID"
                        className="h-11 w-full rounded-md border border-gray-300 px-3 text-sm outline-none transition focus:border-gray-900 focus:ring-2 focus:ring-gray-200"
                    />

                    <div className="mt-2 flex justify-between">
                        <span className="text-xs text-gray-500">
                            17 digits required
                        </span>

                        <span className="text-xs text-gray-500">
                            {value.length}/17
                        </span>
                    </div>

                    {error && (
                        <p className="mt-2 text-sm text-red-600" role="alert">
                            {error}
                        </p>
                    )}
                </div>

                <button
                    type="submit"
                    className="h-11 w-full rounded-md bg-gray-900 text-sm font-medium text-white transition hover:bg-gray-800"
                >
                    Continue
                </button>

                {onCancel && (
                    <button
                        type="button"
                        onClick={onCancel}
                        className="w-full text-sm text-gray-500 hover:text-gray-900"
                    >
                        Cancel
                    </button>
                )}
            </form>
        </div>
    );
}
