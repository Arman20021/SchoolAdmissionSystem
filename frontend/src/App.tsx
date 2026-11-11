
 
import { useNavigate } from "react-router-dom";
import {
  
    createClass,
} from "@/services/classService";


import { useEffect, useState, useRef } from "react";
import { getClasses, type ClassResponse } from "@/services/classService";
import axios from "axios";
import {
    getMyHolds,

    getHoldById,
    type StudentHoldResponse,
} from "@/services/admissionHoldService";
import {
  
    type CreateHoldResponse,
} from "@/services/admissionHoldService";

import {
    createHold,
    confirmHold,
    cancelHold,
} from "@/services/admissionHoldService";

import { useStudentIdentity } from "./features/student/StudentIdentityContext";
import StudentIdentityForm from "./components/student/StudentIdentityForm";
import {
    BrowserRouter,
    Routes,
    Route,
    Link,
    NavLink,
    Outlet,
    useParams,
} from "react-router-dom";


function StudentLayout() {
    const { studentId } = useStudentIdentity();
    const [isChangingId, setIsChangingId] = useState(false);

    const showIdentityForm = !studentId || isChangingId;

    return (
        <div className="min-h-screen bg-white text-gray-900">
            <header className="border-b border-gray-200">
                <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-6">
                    <Link to="/" className="text-lg font-semibold">
                        Admission System
                    </Link>

                    {!showIdentityForm && (
                        <nav className="flex items-center gap-6 text-sm">
                            <NavLink to="/" end>
                                Classes
                            </NavLink>

                            <NavLink to="/my-applications">
                                My Applications
                            </NavLink>

                            <Link to="/admin" className="text-blue-600">
                                Admin
                            </Link>
                        </nav>
                    )}
                </div>
            </header>

            <main className="mx-auto max-w-6xl px-6 py-10">
                {showIdentityForm ? (
                    <StudentIdentityForm
                        onSuccess={() => setIsChangingId(false)}
                        onCancel={
                            studentId
                                ? () => setIsChangingId(false)
                                : undefined
                        }
                    />
                ) : (
                    <>
                        <div className="mb-8 flex items-center justify-between rounded-lg border border-gray-200 px-4 py-3">
                            <div>
                                <p className="text-xs text-gray-500">
                                    Logged in as
                                </p>

                                <p className="text-sm font-medium">
                                    Student ID: •••••••••••••{studentId?.slice(-4)}
                                </p>
                            </div>

                            <button
                                type="button"
                                onClick={() => setIsChangingId(true)}
                                className="text-sm font-medium text-blue-600 hover:text-blue-700"
                            >
                                Change ID
                            </button>
                        </div>

                        <Outlet />
                    </>
                )}
            </main>
        </div>
    );
}

function AdminLayout() {
    return (
        <div className="min-h-screen bg-gray-50 text-gray-900">
            <header className="border-b border-gray-200 bg-white">
                <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-6">
                    <Link to="/admin" className="text-lg font-semibold">
                        Admission Admin
                    </Link>

                    <nav className="flex items-center gap-6 text-sm">
                        <NavLink to="/admin" end>
                            Dashboard
                        </NavLink>

                        <NavLink to="/admin/classes">
                            Classes
                        </NavLink>

                        <NavLink to="/admin/classes/create">
                            Create Class
                        </NavLink>

                        <Link to="/" className="text-blue-600">
                            Student Portal
                        </Link>
                    </nav>
                </div>
            </header>

            <main className="mx-auto max-w-6xl px-6 py-10">
                <Outlet />
            </main>
        </div>
    );
}

// Student pages




function StudentHomePage() {
    const [classes, setClasses] = useState<ClassResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    const [applyingClassId, setApplyingClassId] = useState<string | null>(null);

    const [applicationErrors, setApplicationErrors] = useState<
        Record<string, string>
    >({});




    const applicationErrorTimers = useRef<Map<string, number>>(
        new Map()
    );

    const showApplicationError = (
        classId: string,
        message: string
    ) => {
        // Clear previous timer for this class
        const previousTimer =
            applicationErrorTimers.current.get(classId);

        if (previousTimer !== undefined) {
            window.clearTimeout(previousTimer);
        }

        // Show warning
        setApplicationErrors((previous) => ({
            ...previous,
            [classId]: message,
        }));

        // Remove warning after 2 seconds
        const timerId = window.setTimeout(() => {
            setApplicationErrors((previous) => ({
                ...previous,
                [classId]: "",
            }));

            applicationErrorTimers.current.delete(classId);
        }, 2000);

        applicationErrorTimers.current.set(classId, timerId);
    };

    // Clean up timers when leaving the page
    useEffect(() => {
        return () => {
            applicationErrorTimers.current.forEach((timerId) => {
                window.clearTimeout(timerId);
            });

            applicationErrorTimers.current.clear();
        };
    }, []);



    const [applicationMessages, setApplicationMessages] = useState<
        Record<string, string>
    >({});

    const [createdHolds, setCreatedHolds] = useState<
        Record<string, CreateHoldResponse>
    >({});

    const [confirmedHolds, setConfirmedHolds] = useState<
        Record<string, CreateHoldResponse>
    >({});

    const [currentTime, setCurrentTime] = useState(Date.now());

    // Initial class loading
    const fetchClasses = async () => {
        try {
            setLoading(true);
            setError("");

            const data = await getClasses();
            setClasses(data);
        } catch (err) {
            if (axios.isAxiosError(err)) {
                const problem = err.response?.data as {
                    title?: string;
                    detail?: string;
                } | undefined;

                setError(
                    problem?.detail ||
                    problem?.title ||
                    err.message ||
                    "Unable to load available classes."
                );
            } else {
                setError("Something went wrong. Please try again.");
            }
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        void fetchClasses();
    }, []);

    // Automatic seat and expiration updates
    useEffect(() => {
        let isMounted = true;
        let isRefreshing = false;

        const refreshClasses = async () => {
            if (!isMounted) return;

            const now = Date.now();

            setCurrentTime(now);

            // Immediately remove expired holds
            setCreatedHolds((previous) => {
                return Object.fromEntries(
                    Object.entries(previous).filter(([, hold]) => {
                        const expiresAt = new Date(hold.expiresAt).getTime();

                        return Number.isFinite(expiresAt) && expiresAt > now;
                    })
                );
            });

            if (isRefreshing) return;

            isRefreshing = true;

            try {
                const data = await getClasses();

                if (isMounted) {
                    setClasses(data);
                }
            } catch (err) {
                console.error("Automatic class refresh failed:", err);
            } finally {
                isRefreshing = false;
            }
        };

        const intervalId = window.setInterval(
            refreshClasses,
            1000
        );

        return () => {
            isMounted = false;
            window.clearInterval(intervalId);
        };
    }, []);





    // Create admission hold
    const handleApply = async (classId: string) => {
        try {
            setApplyingClassId(classId);

            setApplicationErrors((previous) => ({
                ...previous,
                [classId]: "",
            }));



            setApplicationMessages((previous) => ({
                ...previous,
                [classId]: "",
            }));

            const hold = await createHold(classId);

            setCreatedHolds((previous) => ({
                ...previous,
                [classId]: hold,
            }));

            const updatedClasses = await getClasses();
            setClasses(updatedClasses);

        }
 


        
 catch (err) {
        let errorMessage = "Something went wrong. Please try again.";

        if (axios.isAxiosError(err)) {
            const problem = err.response?.data as {
                title?: string;
                detail?: string;
            } | undefined;

            errorMessage =
                problem?.detail ||
                problem?.title ||
                (err.response?.status === 409
                    ? "No seats available or you already have an active application."
                    : "Unable to submit your application.");
        }

        showApplicationError(classId, errorMessage);

    } finally {
        setApplyingClassId(null);
    }

    };

    // Confirm admission
    const handleConfirm = async (
        classId: string,
        hold: CreateHoldResponse
    ) => {
        try {
            setApplyingClassId(classId);

            setApplicationErrors((previous) => ({
                ...previous,
                [classId]: "",
            }));

            await confirmHold(hold.holdId);

            setConfirmedHolds((previous) => ({
                ...previous,
                [classId]: hold,
            }));

            setCreatedHolds((previous) => {
                const updated = { ...previous };
                delete updated[classId];
                return updated;
            });

            setApplicationMessages((previous) => ({
                ...previous,
                [classId]: "Admission confirmed successfully!",
            }));

            const updatedClasses = await getClasses();
            setClasses(updatedClasses);

        } catch (err) {
            if (axios.isAxiosError(err)) {
                const problem = err.response?.data as {
                    title?: string;
                    detail?: string;
                } | undefined;

                setApplicationErrors((previous) => ({
                    ...previous,
                    [classId]:
                        problem?.detail ||
                        problem?.title ||
                        "Unable to confirm admission.",
                }));
            } else {
                setApplicationErrors((previous) => ({
                    ...previous,
                    [classId]: "Something went wrong.",
                }));
            }
        } finally {
            setApplyingClassId(null);
        }
    };

    // Cancel admission hold
    const handleCancel = async (classId: string, hold: CreateHoldResponse) => {
        const confirmed = window.confirm(
            "Are you sure you want to cancel this admission hold?"
        );

        if (!confirmed) return;

        try {
            setApplyingClassId(classId);

            setApplicationErrors((previous) => ({
                ...previous,
                [classId]: "",
            }));

            await cancelHold(hold.holdId);

            setCreatedHolds((previous) => {
                const updated = { ...previous };
                delete updated[classId];
                return updated;
            });

            setApplicationMessages((previous) => ({
                ...previous,
                [classId]: "Application cancelled. You can apply again.",
            }));

            const updatedClasses = await getClasses();
            setClasses(updatedClasses);

        } catch (err) {
            if (axios.isAxiosError(err)) {
                const problem = err.response?.data as {
                    title?: string;
                    detail?: string;
                } | undefined;

                setApplicationErrors((previous) => ({
                    ...previous,
                    [classId]:
                        problem?.detail ||
                        problem?.title ||
                        "Unable to cancel application.",
                }));
            } else {
                setApplicationErrors((previous) => ({
                    ...previous,
                    [classId]: "Something went wrong.",
                }));
            }
        } finally {
            setApplyingClassId(null);
        }
    };

    return (
        <section>
            {/* Page Header */}
            <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
                <div>
                    <h1 className="text-2xl font-semibold tracking-tight">
                        Available Classes
                    </h1>

 
                </div>

                <button
                    type="button"
                    onClick={fetchClasses}
                    disabled={loading}
                    className="rounded-md border border-gray-300 px-4 py-2 text-sm font-medium transition hover:bg-gray-50 disabled:opacity-50"
                >
                    {loading ? "Loading..." : "Refresh"}
                </button>
            </div>

            {/* Loading */}
            {loading && (
                <div className="py-16 text-center">
                    <div className="mx-auto mb-4 size-8 animate-spin rounded-full border-2 border-gray-200 border-t-gray-900" />

                    <p className="text-sm text-gray-500">
                        Loading available classes...
                    </p>
                </div>
            )}

            {/* Error */}
            {!loading && error && (
                <div className="rounded-lg border border-red-200 bg-red-50 p-5">
                    <h3 className="font-medium text-red-800">
                        Unable to load classes
                    </h3>

                    <p className="mt-1 text-sm text-red-700">
                        {error}
                    </p>

                    <button
                        type="button"
                        onClick={fetchClasses}
                        className="mt-4 text-sm font-medium text-red-800 underline"
                    >
                        Try again
                    </button>
                </div>
            )}

            {/* Empty State */}
            {!loading && !error && classes.length === 0 && (
                <div className="rounded-lg border border-dashed border-gray-300 py-16 text-center">
                    <h3 className="font-medium text-gray-900">
                        No classes available
                    </h3>

                    <p className="mt-2 text-sm text-gray-500">
                        There are currently no admission classes available.
                    </p>
                </div>
            )}

            {/* Class List */}
            {!loading && !error && classes.length > 0 && (
                <>
                    <p className="mb-5 text-sm text-gray-500">
                        {classes.length} {classes.length === 1 ? "class" : "classes"} found
                    </p>

                    <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
                        {classes.map((item) => {
                            const isAvailable = item.availableSeats > 0;
                            const hold = createdHolds[item.id];
                            const confirmedHold = confirmedHolds[item.id];

                            const isApplying = applyingClassId === item.id;

                            const applicationError = applicationErrors[item.id];
                            const applicationMessage = applicationMessages[item.id];

                            const expiresAt = hold
                                ? new Date(hold.expiresAt).getTime()
                                : 0;

                            const remainingSeconds = hold
                                ? Math.max(
                                    0,
                                    Math.ceil((expiresAt - currentTime) / 1000)
                                )
                                : 0;

                            const minutes = Math.floor(remainingSeconds / 60);
                            const seconds = remainingSeconds % 60;

                            return (
                                <article
                                    key={item.id}
                                    className="rounded-xl border border-gray-200 bg-white p-5 transition hover:border-gray-300"
                                >
                                    {/* Header */}
                                    <div className="flex items-start justify-between gap-3">
                                        <h2 className="text-lg font-semibold text-gray-900">
                                            {item.name}
                                        </h2>

                                        <span
                                            className={`shrink-0 rounded-full px-3 py-1 text-xs font-medium ${isAvailable
                                                    ? "bg-green-50 text-green-700"
                                                    : "bg-red-50 text-red-700"
                                                }`}
                                        >
                                            {isAvailable ? "Available" : "Full"}
                                        </span>
                                    </div>

                                    {/* Seat Statistics */}
                                    <div className="mt-5 grid grid-cols-2 gap-3">
                                        <div className="rounded-xl border border-blue-100 bg-blue-50/70 px-4 py-3">
                                            <p className="text-sm font-medium text-blue-700">
                                                Total Seats
                                            </p>

                                            <p className="mt-2 text-3xl font-bold text-blue-950">
                                                {item.totalSeats}
                                            </p>
                                        </div>

                                        <div className="rounded-xl border border-emerald-100 bg-emerald-50/70 px-4 py-3">
                                            <p className="text-sm font-medium text-emerald-700">
                                                Available Seats
                                            </p>

                                            <p className="mt-2 text-3xl font-bold text-emerald-950">
                                                {item.availableSeats}
                                            </p>
                                        </div>

                                        <div className="rounded-xl border border-amber-100 bg-amber-50/70 px-4 py-3">
                                            <p className="text-sm font-medium text-amber-700">
                                                Held Seats
                                            </p>

                                            <p className="mt-2 text-2xl font-semibold text-amber-950">
                                                {item.heldSeats}
                                            </p>
                                        </div>

                                        <div className="rounded-xl border border-violet-100 bg-violet-50/70 px-4 py-3">
                                            <p className="text-sm font-medium text-violet-700">
                                                Confirmed Seats
                                            </p>

                                            <p className="mt-2 text-2xl font-semibold text-violet-950">
                                                {item.confirmedSeats}
                                            </p>
                                        </div>
                                    </div>

                                    {/* Application Error */}
                                    {applicationError && (
                                        <div className="mt-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3">
                                            <p className="text-sm text-red-700">
                                                {applicationError}
                                            </p>
                                        </div>
                                    )}

                                    {/* Application Message */}
                                    {applicationMessage && (
                                        <div className="mt-4 rounded-lg border border-green-200 bg-green-50 px-4 py-3">
                                            <p className="text-sm font-medium text-green-800">
                                                {applicationMessage}
                                            </p>
                                        </div>
                                    )}

                                    {/* Active Hold: Countdown + Confirm / Cancel */}
                                    {hold && (
                                        <div className="mt-4 rounded-lg border border-amber-200 bg-amber-50 p-4">
                                            <div className="flex items-center justify-between gap-3">
                                                <p className="font-semibold text-amber-900">
                                                    Seat temporarily reserved
                                                </p>

                                                <span className="rounded-md bg-white px-3 py-1 font-mono text-sm font-bold text-amber-800">
                                                    {String(minutes).padStart(2, "0")}:
                                                    {String(seconds).padStart(2, "0")}
                                                </span>
                                            </div>

                                            <p className="mt-2 text-sm text-amber-800">
                                                Confirm your admission before the reservation expires.
                                            </p>

                                            {/*<p className="mt-2 break-all text-xs text-amber-700">*/}
                                            {/*    Hold ID: {hold.holdId}*/}
                                            {/*</p>*/}

                                            <div className="mt-4 grid grid-cols-2 gap-3">
                                                <button
                                                    type="button"
                                                    onClick={() => handleConfirm(item.id, hold)}
                                                    disabled={isApplying || remainingSeconds <= 0}
                                                    className="h-11 rounded-lg bg-green-600 px-3 text-sm font-semibold text-white transition hover:bg-green-700 disabled:cursor-not-allowed disabled:opacity-50"
                                                >
                                                    {isApplying ? "Processing..." : "✓ Confirm"}
                                                </button>

                                                <button
                                                    type="button"
                                                    onClick={() => handleCancel(item.id, hold)}
                                                    disabled={isApplying}
                                                    className="h-11 rounded-lg border border-red-200 bg-white px-3 text-sm font-semibold text-red-600 transition hover:bg-red-50 disabled:opacity-50"
                                                >
                                                    Cancel
                                                </button>
                                            </div>
                                        </div>
                                    )}

                                    {/* Confirmed Application */}
                                    {confirmedHold && (
                                        <div className="mt-4 rounded-lg border border-green-200 bg-green-50 p-4">
                                            <p className="font-semibold text-green-800">
                                                ✓ Admission Confirmed
                                            </p>

                                            <p className="mt-2 break-all text-sm text-green-700">
                                                Hold ID: {confirmedHold.holdId}
                                            </p>

                                            <p className="mt-2 text-sm text-green-700">
                                                Your admission has been successfully confirmed.
                                            </p>
                                        </div>
                                    )}

                                    {/* Apply Button */}
                                    {!hold && !confirmedHold && (
                                        <button
                                            type="button"
                                            onClick={() => handleApply(item.id)}
                                            disabled={
                                                !isAvailable ||
                                                applyingClassId !== null
                                            }
                                            className="mt-5 h-11 w-full rounded-lg bg-gray-900 px-4 text-sm font-medium text-white transition hover:bg-gray-800 disabled:cursor-not-allowed disabled:bg-gray-300 disabled:text-gray-600"
                                        >
                                            {isApplying
                                                ? "Submitting Application..."
                                                : isAvailable
                                                    ? "Apply for Admission"
                                                    : "No Seats Available"}
                                        </button>
                                    )}
                                </article>
                            );
                        })}
                    </div>
                </>
            )}
        </section>
    );
}

/*function MyApplicationsPage() {
    return (
        <section>
            <h1 className="text-2xl font-semibold">
                My Applications
            </h1>

            <p className="mt-2 text-sm text-gray-500">
                View and manage your admission applications.
            </p>
        </section>
    );
}
*/

function MyApplicationsPage() {
    const [applications, setApplications] = useState<StudentHoldResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [actionError, setActionError] = useState("");
    const [notice, setNotice] = useState("");
    const [actionHoldId, setActionHoldId] = useState<string | null>(null);

    // Fetch applications
    const fetchApplications = async () => {
        try {
            const data = await getMyHolds();
            setApplications(data);
            setError("");
        } catch (err) {
            if (axios.isAxiosError(err)) {
                const problem = err.response?.data as {
                    title?: string;
                    detail?: string;
                } | undefined;

                setError(
                    problem?.detail ||
                    problem?.title ||
                    "Unable to load your applications."
                );
            } else {
                setError("Something went wrong.");
            }
        } finally {
            setLoading(false);
        }
    };

    // Initial loading and automatic updates
    useEffect(() => {
        let mounted = true;
        let refreshing = false;

        const loadApplications = async () => {
            if (refreshing) return;

            refreshing = true;

            try {
                const data = await getMyHolds();

                if (mounted) {
                    setApplications(data);
                    setError("");
                }
            } catch (err) {
                if (mounted && applications.length === 0) {
                    setError("Unable to load your applications.");
                }

                console.error("Application refresh failed:", err);
            } finally {
                refreshing = false;

                if (mounted) {
                    setLoading(false);
                }
            }
        };

        void loadApplications();

        const intervalId = window.setInterval(
            loadApplications,
            5000
        );

        return () => {
            mounted = false;
            window.clearInterval(intervalId);
        };
    }, []);

    // Confirm application
    const handleConfirm = async (holdId: string) => {
        try {
            setActionHoldId(holdId);
            setActionError("");
            setNotice("");

            await confirmHold(holdId);

            setNotice("Your admission application has been confirmed.");

            await fetchApplications();
        } catch (err) {
            if (axios.isAxiosError(err)) {
                const problem = err.response?.data as {
                    title?: string;
                    detail?: string;
                } | undefined;

                setActionError(
                    problem?.detail ||
                    problem?.title ||
                    "Unable to confirm your application."
                );
            } else {
                setActionError("Something went wrong.");
            }
        } finally {
            setActionHoldId(null);
        }
    };

    // Cancel application
    const handleCancel = async (holdId: string) => {
        const confirmed = window.confirm(
            "Are you sure you want to cancel this application?"
        );

        if (!confirmed) return;

        try {
            setActionHoldId(holdId);
            setActionError("");
            setNotice("");

            await cancelHold(holdId);

            setNotice("Your application has been cancelled.");

            await fetchApplications();
        } catch (err) {
            if (axios.isAxiosError(err)) {
                const problem = err.response?.data as {
                    title?: string;
                    detail?: string;
                } | undefined;

                setActionError(
                    problem?.detail ||
                    problem?.title ||
                    "Unable to cancel your application."
                );
            } else {
                setActionError("Something went wrong.");
            }
        } finally {
            setActionHoldId(null);
        }
    };

    const getStatusStyle = (status: string) => {
        switch (status.toUpperCase()) {
            case "HELD":
                return "bg-amber-50 text-amber-700 border-amber-100";

            case "CONFIRMED":
                return "bg-emerald-50 text-emerald-700 border-emerald-100";

            case "EXPIRED":
                return "bg-gray-100 text-gray-600 border-gray-200";

            case "CANCELLED":
                return "bg-red-50 text-red-700 border-red-100";

            default:
                return "bg-gray-100 text-gray-600 border-gray-200";
        }
    };

    return (
        <section>
            {/* Header */}
            <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
                <div>
                    <h1 className="text-2xl font-semibold tracking-tight">
                        My Applications
                    </h1>

                    <p className="mt-2 text-sm text-gray-500">
                        Track and manage your admission applications.
                    </p>
                </div>

                <button
                    type="button"
                    onClick={fetchApplications}
                    disabled={loading}
                    className="rounded-md border border-gray-300 px-4 py-2 text-sm font-medium transition hover:bg-gray-50 disabled:opacity-50"
                >
                    Refresh
                </button>
            </div>

            {/* Success Message */}
            {notice && (
                <div className="mb-5 rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-700">
                    {notice}
                </div>
            )}

            {/* Action Error */}
            {actionError && (
                <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                    {actionError}
                </div>
            )}

            {/* Loading */}
            {loading && (
                <div className="py-16 text-center">
                    <div className="mx-auto mb-4 size-8 animate-spin rounded-full border-2 border-gray-200 border-t-gray-900" />

                    <p className="text-sm text-gray-500">
                        Loading applications...
                    </p>
                </div>
            )}

            {/* Error */}
            {!loading && error && (
                <div className="rounded-lg border border-red-200 bg-red-50 p-5">
                    <p className="text-sm text-red-700">{error}</p>

                    <button
                        type="button"
                        onClick={fetchApplications}
                        className="mt-3 text-sm font-medium text-red-800 underline"
                    >
                        Try again
                    </button>
                </div>
            )}

            {/* Empty State */}
            {!loading && !error && applications.length === 0 && (
                <div className="rounded-xl border border-dashed border-gray-300 py-16 text-center">
                    <h3 className="font-semibold text-gray-900">
                        No applications yet
                    </h3>

                    <p className="mt-2 text-sm text-gray-500">
                        You haven't submitted any admission applications.
                    </p>

                    <Link
                        to="/"
                        className="mt-5 inline-block rounded-lg bg-gray-900 px-5 py-2.5 text-sm font-medium text-white hover:bg-gray-800"
                    >
                        Browse Classes
                    </Link>
                </div>
            )}

            {/* Application List */}
            {!loading && !error && applications.length > 0 && (
                <div className="space-y-4">
                    <p className="text-sm text-gray-500">
                        {applications.length} applications found
                    </p>

                    {applications.map((application) => {
                        const isHeld =
                            application.status.toUpperCase() === "HELD";

                        const isBusy =
                            actionHoldId === application.holdId;

                        return (
                            <article
                                key={application.holdId}
                                className="rounded-xl border border-gray-200 bg-white p-5"
                            >
                                {/* Application Header */}
                                <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
                                    <div>
                                        <h2 className="text-lg font-semibold text-gray-900">
                                            {application.className}
                                        </h2>

                                        <p className="mt-1 text-xs text-gray-500">
                                            Hold ID: {application.holdId}
                                        </p>
                                    </div>

                                    <span
                                        className={`inline-flex w-fit items-center rounded-full border px-3 py-1 text-xs font-medium ${getStatusStyle(application.status)}`}
                                    >
                                        {application.status}
                                    </span>
                                </div>

                                {/* Expiration */}
                                <div className="mt-5 rounded-lg bg-gray-50 p-4">
                                    <p className="text-xs text-gray-500">
                                        Hold Expiration
                                    </p>

                                    <p className="mt-1 text-sm font-medium text-gray-900">
                                        {new Date(application.expiresAt).toLocaleString()}
                                    </p>
                                </div>

                                {/* Actions */}
                                {isHeld && (
                                    <div className="mt-5 flex flex-col gap-3 sm:flex-row">
                                        <button
                                            type="button"
                                            onClick={() =>
                                                handleConfirm(application.holdId)
                                            }
                                            disabled={actionHoldId !== null}
                                            className="flex-1 rounded-lg bg-gray-900 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-gray-800 disabled:opacity-50"
                                        >
                                            {isBusy ? "Processing..." : "Confirm Admission"}
                                        </button>

                                        <button
                                            type="button"
                                            onClick={() =>
                                                handleCancel(application.holdId)
                                            }
                                            disabled={actionHoldId !== null}
                                            className="flex-1 rounded-lg border border-red-200 px-4 py-2.5 text-sm font-medium text-red-600 transition hover:bg-red-50 disabled:opacity-50"
                                        >
                                            Cancel Application
                                        </button>
                                    </div>
                                )}

                                {/* Details */}
                                <div className="mt-4">
                                    <Link
                                        to={`/applications/${encodeURIComponent(application.holdId)}`}
                                        className="text-sm font-medium text-blue-600 hover:underline"
                                    >
                                        View Application Details →
                                    </Link>
                                </div>
                            </article>
                        );
                    })}
                </div>
            )}
        </section>
    );
}




function ApplicationDetailsPage() {
    const { holdId } = useParams<{ holdId: string }>();

    const [application, setApplication] = useState<
        Awaited<ReturnType<typeof getHoldById>> | null
    >(null);

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [actionError, setActionError] = useState("");
    const [notice, setNotice] = useState("");
    const [processing, setProcessing] = useState(false);

    // Fetch application details
    const fetchApplication = async (showLoading = false) => {
        if (!holdId) {
            setError("Invalid application ID.");
            setLoading(false);
            return;
        }

        try {
            if (showLoading) setLoading(true);

            const data = await getHoldById(holdId);

            setApplication(data);
            setError("");
        } catch (err) {
            if (axios.isAxiosError(err)) {
                const problem = err.response?.data as {
                    title?: string;
                    detail?: string;
                } | undefined;

                setError(
                    problem?.detail ||
                    problem?.title ||
                    "Unable to load application details."
                );
            } else {
                setError("Something went wrong.");
            }
        } finally {
            setLoading(false);
        }
    };

    // Initial loading and automatic refresh
    useEffect(() => {
        let mounted = true;
        let refreshing = false;

        const loadApplication = async () => {
            if (!holdId || refreshing) return;

            refreshing = true;

            try {
                const data = await getHoldById(holdId);

                if (mounted) {
                    setApplication(data);
                    setError("");
                }
            } catch (err) {
                if (mounted && !application) {
                    setError("Unable to load application details.");
                }
            } finally {
                refreshing = false;

                if (mounted) {
                    setLoading(false);
                }
            }
        };

        void loadApplication();

        const intervalId = window.setInterval(
            loadApplication,
            5000
        );

        return () => {
            mounted = false;
            window.clearInterval(intervalId);
        };
    }, [holdId]);

    // Confirm admission
    const handleConfirm = async () => {
        if (!holdId) return;

        try {
            setProcessing(true);
            setActionError("");
            setNotice("");

            await confirmHold(holdId);

            setNotice("Your admission has been confirmed.");

            await fetchApplication();
        } catch (err) {
            if (axios.isAxiosError(err)) {
                const problem = err.response?.data as {
                    title?: string;
                    detail?: string;
                } | undefined;

                setActionError(
                    problem?.detail ||
                    problem?.title ||
                    "Unable to confirm admission."
                );
            } else {
                setActionError("Something went wrong.");
            }
        } finally {
            setProcessing(false);
        }
    };

    // Cancel application
    const handleCancel = async () => {
        if (!holdId) return;

        const confirmed = window.confirm(
            "Are you sure you want to cancel this application?"
        );

        if (!confirmed) return;

        try {
            setProcessing(true);
            setActionError("");
            setNotice("");

            await cancelHold(holdId);

            setNotice("Your application has been cancelled.");

            await fetchApplication();
        } catch (err) {
            if (axios.isAxiosError(err)) {
                const problem = err.response?.data as {
                    title?: string;
                    detail?: string;
                } | undefined;

                setActionError(
                    problem?.detail ||
                    problem?.title ||
                    "Unable to cancel application."
                );
            } else {
                setActionError("Something went wrong.");
            }
        } finally {
            setProcessing(false);
        }
    };

    // Status colors
    const getStatusStyle = (status: string) => {
        switch (status.toUpperCase()) {
            case "HELD":
                return "border-amber-200 bg-amber-50 text-amber-700";

            case "CONFIRMED":
                return "border-emerald-200 bg-emerald-50 text-emerald-700";

            case "EXPIRED":
                return "border-gray-200 bg-gray-100 text-gray-600";

            case "CANCELLED":
                return "border-red-200 bg-red-50 text-red-700";

            default:
                return "border-gray-200 bg-gray-100 text-gray-600";
        }
    };

    if (loading) {
        return (
            <div className="py-16 text-center">
                <div className="mx-auto mb-4 size-8 animate-spin rounded-full border-2 border-gray-200 border-t-gray-900" />

                <p className="text-sm text-gray-500">
                    Loading application details...
                </p>
            </div>
        );
    }

    if (error || !application) {
        return (
            <section className="py-12 text-center">
                <h1 className="text-xl font-semibold">
                    Application not found
                </h1>

                <p className="mt-2 text-sm text-gray-500">
                    {error || "This application could not be found."}
                </p>

                <Link
                    to="/my-applications"
                    className="mt-5 inline-block text-sm font-medium text-blue-600 hover:underline"
                >
                    Back to My Applications
                </Link>
            </section>
        );
    }

    const isHeld = application.status.toUpperCase() === "HELD";

    return (
        <section className="mx-auto max-w-3xl">

            {/* Header */}
            <div className="mb-8">
                <Link
                    to="/my-applications"
                    className="text-sm text-gray-500 hover:text-gray-900"
                >
                    ← Back to My Applications
                </Link>

                <h1 className="mt-5 text-2xl font-semibold tracking-tight">
                    Application Details
                </h1>

                <p className="mt-2 text-sm text-gray-500">
                    View and manage your admission application.
                </p>
            </div>

            {/* Notifications */}
            {notice && (
                <div className="mb-5 rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-700">
                    {notice}
                </div>
            )}

            {actionError && (
                <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                    {actionError}
                </div>
            )}

            {/* Application Card */}
            <div className="rounded-xl border border-gray-200 bg-white p-6">

                {/* Status */}
                <div className="flex items-center justify-between gap-4">
                    <h2 className="text-lg font-semibold">
                        Application Information
                    </h2>

                    <span
                        className={`rounded-full border px-3 py-1 text-xs font-medium ${getStatusStyle(application.status)}`}
                    >
                        {application.status}
                    </span>
                </div>

                {/* Details */}
                <div className="mt-7 grid gap-6 sm:grid-cols-2">

                    <div>
                        <p className="text-sm text-gray-500">
                            Hold ID
                        </p>

                        <p className="mt-2 break-all text-sm font-medium">
                            {application.holdId}
                        </p>
                    </div>

                    <div>
                        <p className="text-sm text-gray-500">
                            Class ID
                        </p>

                        <p className="mt-2 text-sm font-medium">
                            {application.classId}
                        </p>
                    </div>

                    <div className="sm:col-span-2">
                        <p className="text-sm text-gray-500">
                            Expiration Date
                        </p>

                        <p className="mt-2 text-sm font-medium">
                            {new Date(application.expiresAt).toLocaleString()}
                        </p>
                    </div>

                </div>

                {/* Actions */}
                {isHeld && (
                    <div className="mt-8 flex flex-col gap-3 border-t border-gray-100 pt-6 sm:flex-row">

                        <button
                            type="button"
                            onClick={handleConfirm}
                            disabled={processing}
                            className="flex-1 rounded-lg bg-gray-900 px-5 py-3 text-sm font-medium text-white transition hover:bg-gray-800 disabled:opacity-50"
                        >
                            {processing
                                ? "Processing..."
                                : "Confirm Admission"}
                        </button>

                        <button
                            type="button"
                            onClick={handleCancel}
                            disabled={processing}
                            className="flex-1 rounded-lg border border-red-200 px-5 py-3 text-sm font-medium text-red-600 transition hover:bg-red-50 disabled:opacity-50"
                        >
                            Cancel Application
                        </button>

                    </div>
                )}

                {/* Status Information */}
                {!isHeld && (
                    <div className="mt-8 rounded-lg bg-gray-50 p-4">
                        <p className="text-sm text-gray-600">
                            {application.status.toUpperCase() === "CONFIRMED"
                                ? "Your admission has been confirmed."
                                : application.status.toUpperCase() === "EXPIRED"
                                    ? "Your admission hold has expired. You can apply again if seats are available."
                                    : "This application is no longer active."}
                        </p>
                    </div>
                )}

            </div>
        </section>
    );
}


// Admin pages


function AdminDashboardPage() {
    const [classes, setClasses] = useState<ClassResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    useEffect(() => {
        let isMounted = true;
        let isFetching = false;

        const fetchDashboardData = async () => {
            if (isFetching) return;

            isFetching = true;

            try {
                const data = await getClasses();

                if (isMounted) {
                    setClasses(data);
                    setError("");
                }
            } catch {
                if (isMounted) {
                    setError("Unable to refresh dashboard data.");
                }
            } finally {
                isFetching = false;

                if (isMounted) {
                    setLoading(false);
                }
            }
        };

        void fetchDashboardData();

        const interval = window.setInterval(() => {
            void fetchDashboardData();
        }, 5000);

        return () => {
            isMounted = false;
            window.clearInterval(interval);
        };
    }, []);

    const statistics = [
        {
            label: "Total Classes",
            value: classes.length,
            color: "bg-blue-50 border-blue-100 text-blue-700",
        },
        {
            label: "Total Seats",
            value: classes.reduce((sum, item) => sum + item.totalSeats, 0),
            color: "bg-violet-50 border-violet-100 text-violet-700",
        },
        {
            label: "Held Seats",
            value: classes.reduce((sum, item) => sum + item.heldSeats, 0),
            color: "bg-amber-50 border-amber-100 text-amber-700",
        },
        {
            label: "Confirmed Seats",
            value: classes.reduce((sum, item) => sum + item.confirmedSeats, 0),
            color: "bg-green-50 border-green-100 text-green-700",
        },
        {
            label: "Available Seats",
            value: classes.reduce((sum, item) => sum + item.availableSeats, 0),
            color: "bg-cyan-50 border-cyan-100 text-cyan-700",
        },
    ];

    return (
        <section className="space-y-8">

            {/* Header */}
            <div>
                <h1 className="text-2xl font-semibold tracking-tight">
                    Dashboard
                </h1>

                <p className="mt-1 text-sm text-muted-foreground">
                    Overview of admission classes and seat statistics.
                </p>
            </div>

            {/* Error */}
            {error && (
                <p className="text-sm text-red-600">
                    {error}
                </p>
            )}

            {/* Statistics */}
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-5">
                {statistics.map((item) => (
                    <div
                        key={item.label}
                        className={`rounded-xl border p-5 ${item.color}`}
                    >
                        <p className="text-sm font-medium opacity-80">
                            {item.label}
                        </p>

                        <p className="mt-3 text-3xl font-semibold tracking-tight">
                            {loading ? "—" : item.value.toLocaleString()}
                        </p>
                    </div>
                ))}
            </div>

            {/* Class Overview */}
            <div className="overflow-hidden rounded-xl border border-gray-200 bg-white">

                <div className="flex items-center justify-between border-b border-gray-200 px-5 py-4">
                    <div>
                        <h2 className="font-semibold">
                            Class Overview
                        </h2>

                        <p className="mt-1 text-xs text-gray-500">
                            Admission seat distribution by class.
                        </p>
                    </div>

                    <Link
                        to="/admin/classes"
                        className="text-sm font-medium text-blue-600 hover:text-blue-700"
                    >
                        View all
                    </Link>
                </div>

                <div className="overflow-x-auto">
                    <table className="w-full text-left text-sm">

                        <thead className="bg-gray-50 text-gray-500">
                            <tr>
                                <th className="px-5 py-3 font-medium">
                                    Class Name
                                </th>

                                <th className="px-5 py-3 text-center font-medium">
                                    Total Seats
                                </th>

                                <th className="px-5 py-3 text-center font-medium">
                                    Held
                                </th>

                                <th className="px-5 py-3 text-center font-medium">
                                    Confirmed
                                </th>

                                <th className="px-5 py-3 text-center font-medium">
                                    Available
                                </th>
                            </tr>
                        </thead>

                        <tbody className="divide-y divide-gray-100">

                            {loading ? (
                                <tr>
                                    <td
                                        colSpan={5}
                                        className="px-5 py-10 text-center text-gray-500"
                                    >
                                        Loading classes...
                                    </td>
                                </tr>
                            ) : classes.length === 0 ? (
                                <tr>
                                    <td
                                        colSpan={5}
                                        className="px-5 py-10 text-center text-gray-500"
                                    >
                                        No classes available.
                                    </td>
                                </tr>
                            ) : (
                                classes.map((item) => (
                                    <tr
                                        key={item.id}
                                        className="transition-colors hover:bg-gray-50"
                                    >
                                        <td className="px-5 py-4 font-medium text-gray-900">
                                            {item.name}
                                        </td>

                                        <td className="px-5 py-4 text-center">
                                            {item.totalSeats}
                                        </td>

                                        <td className="px-5 py-4 text-center text-amber-600">
                                            {item.heldSeats}
                                        </td>

                                        <td className="px-5 py-4 text-center text-green-600">
                                            {item.confirmedSeats}
                                        </td>

                                        <td className="px-5 py-4 text-center font-medium text-blue-600">
                                            {item.availableSeats}
                                        </td>
                                    </tr>
                                ))
                            )}

                        </tbody>
                    </table>
                </div>
            </div>

            {/* Quick Actions */}
            <div>
                <h2 className="mb-4 font-semibold">
                    Quick Actions
                </h2>

                <div className="flex flex-wrap gap-3">

                    <Link
                        to="/admin/classes/create"
                        className="rounded-lg bg-gray-900 px-5 py-3 text-sm font-medium text-white transition hover:bg-gray-700"
                    >
                        + Create New Class
                    </Link>

                    <Link
                        to="/admin/classes"
                        className="rounded-lg border border-gray-200 bg-white px-5 py-3 text-sm font-medium text-gray-700 transition hover:bg-gray-50"
                    >
                        Manage Classes
                    </Link>

                    <Link
                        to="/"
                        className="rounded-lg border border-gray-200 bg-white px-5 py-3 text-sm font-medium text-gray-700 transition hover:bg-gray-50"
                    >
                        Student Portal
                    </Link>

                </div>
            </div>

        </section>
    );
}



function AdminClassesPage() {
    const [classes, setClasses] = useState<ClassResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [search, setSearch] = useState("");
    const [sortBy, setSortBy] = useState("name");

    useEffect(() => {
        let mounted = true;
        let refreshing = false;

        const fetchClasses = async () => {
            if (refreshing) return;

            refreshing = true;

            try {
                const data = await getClasses();

                if (mounted) {
                    setClasses(data);
                    setError("");
                }
            } catch {
                if (mounted) {
                    setError("Unable to load classes.");
                }
            } finally {
                refreshing = false;

                if (mounted) {
                    setLoading(false);
                }
            }
        };

        void fetchClasses();

        const intervalId = window.setInterval(
            fetchClasses,
            5000
        );

        return () => {
            mounted = false;
            window.clearInterval(intervalId);
        };
    }, []);

    const filteredClasses = classes
        .filter((item) =>
            item.name.toLowerCase().includes(search.toLowerCase())
        )
        .sort((a, b) => {
            switch (sortBy) {
                case "name":
                    return a.name.localeCompare(b.name);

                case "available":
                    return b.availableSeats - a.availableSeats;

                case "total":
                    return b.totalSeats - a.totalSeats;

                default:
                    return 0;
            }
        });

    return (
        <section className="space-y-7">

            {/* Header */}
            <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
                <div>
                    <h1 className="text-2xl font-semibold tracking-tight">
                        Manage Classes
                    </h1>

                    <p className="mt-1 text-sm text-gray-500">
                        View and manage admission class availability.
                    </p>
                </div>

                <Link
                    to="/admin/classes/create"
                    className="inline-flex items-center justify-center rounded-lg bg-gray-900 px-5 py-3 text-sm font-medium text-white transition hover:bg-gray-700"
                >
                    + Create New Class
                </Link>
            </div>

            {/* Search and Sort */}
            <div className="flex flex-col gap-3 sm:flex-row">

                <input
                    type="search"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    placeholder="Search classes..."
                    className="h-11 flex-1 rounded-lg border border-gray-200 bg-white px-4 text-sm outline-none transition focus:border-gray-400"
                />

                <select
                    value={sortBy}
                    onChange={(e) => setSortBy(e.target.value)}
                    className="h-11 rounded-lg border border-gray-200 bg-white px-4 text-sm outline-none focus:border-gray-400"
                >
                    <option value="name">Sort by name</option>
                    <option value="available">Most available seats</option>
                    <option value="total">Most total seats</option>
                </select>

            </div>

            {/* Error */}
            {error && (
                <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                    {error}
                </div>
            )}

            {/* Classes Table */}
            <div className="overflow-hidden rounded-xl border border-gray-200 bg-white">

                <div className="flex items-center justify-between border-b border-gray-200 px-5 py-4">
                    <div>
                        <h2 className="font-semibold text-gray-900">
                            All Classes
                        </h2>

                        <p className="mt-1 text-xs text-gray-500">
                            {classes.length} classes registered
                        </p>
                    </div>

                    <span className="rounded-full bg-gray-100 px-3 py-1 text-xs font-medium text-gray-600">
                        Live updates
                    </span>
                </div>

                <div className="overflow-x-auto">
                    <table className="w-full text-left text-sm">

                        <thead className="bg-gray-50 text-gray-500">
                            <tr>
                                <th className="px-5 py-4 font-medium">
                                    Class Name
                                </th>

                                <th className="px-5 py-4 text-center font-medium">
                                    Total
                                </th>

                                <th className="px-5 py-4 text-center font-medium">
                                    Held
                                </th>

                                <th className="px-5 py-4 text-center font-medium">
                                    Confirmed
                                </th>

                                <th className="px-5 py-4 text-center font-medium">
                                    Available
                                </th>

                                <th className="px-5 py-4 text-center font-medium">
                                    Availability
                                </th>
                            </tr>
                        </thead>

                        <tbody className="divide-y divide-gray-100">

                            {loading ? (
                                <tr>
                                    <td
                                        colSpan={6}
                                        className="px-5 py-12 text-center text-gray-500"
                                    >
                                        Loading classes...
                                    </td>
                                </tr>
                            ) : filteredClasses.length === 0 ? (
                                <tr>
                                    <td
                                        colSpan={6}
                                        className="px-5 py-12 text-center"
                                    >
                                        <p className="font-medium text-gray-700">
                                            {search
                                                ? "No matching classes found."
                                                : "No classes created yet."}
                                        </p>

                                        {!search && (
                                            <Link
                                                to="/admin/classes/create"
                                                className="mt-3 inline-block text-sm font-medium text-blue-600 hover:underline"
                                            >
                                                Create your first class
                                            </Link>
                                        )}
                                    </td>
                                </tr>
                            ) : (
                                filteredClasses.map((item) => {
                                    const percentage =
                                        item.totalSeats > 0
                                            ? (item.availableSeats / item.totalSeats) * 100
                                            : 0;

                                    return (
                                        <tr
                                            key={item.id}
                                            className="transition-colors hover:bg-gray-50"
                                        >
                                            <td className="px-5 py-4">
                                                <p className="font-medium text-gray-900">
                                                    {item.name}
                                                </p>
                                            </td>

                                            <td className="px-5 py-4 text-center">
                                                {item.totalSeats}
                                            </td>

                                            <td className="px-5 py-4 text-center text-amber-600">
                                                {item.heldSeats}
                                            </td>

                                            <td className="px-5 py-4 text-center text-green-600">
                                                {item.confirmedSeats}
                                            </td>

                                            <td className="px-5 py-4 text-center font-semibold text-blue-600">
                                                {item.availableSeats}
                                            </td>

                                            <td className="min-w-40 px-5 py-4">
                                                <div className="flex items-center gap-2">
                                                    <div className="h-2 flex-1 overflow-hidden rounded-full bg-gray-100">
                                                        <div
                                                            className="h-full rounded-full bg-blue-500 transition-all duration-500"
                                                            style={{
                                                                width: `${Math.min(100, Math.max(0, percentage))}%`,
                                                            }}
                                                        />
                                                    </div>

                                                    <span className="w-10 text-right text-xs text-gray-500">
                                                        {Math.round(percentage)}%
                                                    </span>
                                                </div>
                                            </td>
                                        </tr>
                                    );
                                })
                            )}

                        </tbody>
                    </table>
                </div>

                {/* Footer */}
                {!loading && filteredClasses.length > 0 && (
                    <div className="border-t border-gray-100 px-5 py-3 text-xs text-gray-500">
                        Showing {filteredClasses.length} of {classes.length} classes
                    </div>
                )}

            </div>

        </section>
    );
}



function CreateClassPage() {
    const navigate = useNavigate();

    const [name, setName] = useState("");
    const [totalSeats, setTotalSeats] = useState("120");

    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");

    const handleSubmit = async (
        event: React.FormEvent<HTMLFormElement>
    ) => {
        event.preventDefault();

        setError("");
        setSuccess("");

        const trimmedName = name.trim();
        const seats = Number(totalSeats);

        // Validate class name
        if (!trimmedName) {
            setError("Class name is required.");
            return;
        }

        if (trimmedName.length > 100) {
            setError("Class name cannot exceed 100 characters.");
            return;
        }

        // Validate seat capacity
        if (
            !Number.isSafeInteger(seats) ||
            seats <= 0
        ) {
            setError("Total seats must be a positive whole number.");
            return;
        }

        try {
            setLoading(true);

            const createdClass = await createClass({
                name: trimmedName,
                totalSeats: seats,
            });

            setSuccess(
                `${createdClass.name} has been created successfully.`
            );

            setName("");
            setTotalSeats("120");

        } catch (err) {
            if (axios.isAxiosError(err)) {
                const response = err.response?.data as {
                    title?: string;
                    detail?: string;
                    message?: string;
                } | undefined;

                if (err.response?.status === 409) {
                    setError(
                        response?.detail ||
                        response?.message ||
                        "A class with this name already exists."
                    );
                } else {
                    setError(
                        response?.detail ||
                        response?.message ||
                        response?.title ||
                        "Unable to create class. Please try again."
                    );
                }
            } else {
                setError("Something went wrong.");
            }
        } finally {
            setLoading(false);
        }
    };

    return (
        <section className="mx-auto max-w-2xl space-y-7">

            {/* Header */}
            <div>
                <button
                    type="button"
                    onClick={() => navigate("/admin/classes")}
                    className="mb-5 text-sm text-gray-500 transition hover:text-gray-900"
                >
                    ← Back to Classes
                </button>

                <h1 className="text-2xl font-semibold tracking-tight">
                    Create Class
                </h1>

                <p className="mt-2 text-sm text-gray-500">
                    Add a new admission class and configure its seat capacity.
                </p>
            </div>

            {/* Form */}
            <form
                onSubmit={handleSubmit}
                className="rounded-xl border border-gray-200 bg-white p-6 sm:p-8"
            >

                <div className="space-y-6">

                    {/* Class Name */}
                    <div>
                        <label
                            htmlFor="className"
                            className="mb-2 block text-sm font-medium text-gray-800"
                        >
                            Class Name
                        </label>

                        <input
                            id="className"
                            type="text"
                            value={name}
                            onChange={(event) => setName(event.target.value)}
                            placeholder="e.g. Class 6 - Morning Shift"
                            maxLength={100}
                            required
                            disabled={loading}
                            className="h-11 w-full rounded-lg border border-gray-200 bg-white px-4 text-sm outline-none transition placeholder:text-gray-400 focus:border-gray-500 disabled:opacity-60"
                        />

                        <p className="mt-2 text-xs text-gray-400">
                            Enter a descriptive name for this admission class.
                        </p>
                    </div>

                    {/* Total Seats */}
                    <div>
                        <label
                            htmlFor="totalSeats"
                            className="mb-2 block text-sm font-medium text-gray-800"
                        >
                            Total Seat Capacity
                        </label>

                        <input
                            id="totalSeats"
                            type="number"
                            value={totalSeats}
                            onChange={(event) =>
                                setTotalSeats(event.target.value)
                            }
                            min="1"
                            step="1"
                            required
                            disabled={loading}
                            placeholder="Enter total seats"
                            className="h-11 w-full rounded-lg border border-gray-200 bg-white px-4 text-sm outline-none transition placeholder:text-gray-400 focus:border-gray-500 disabled:opacity-60"
                        />

                        <p className="mt-2 text-xs text-gray-400">
                            Maximum admission capacity for this class.
                        </p>
                    </div>

                </div>

                {/* Error Message */}
                {error && (
                    <div
                        role="alert"
                        className="mt-6 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700"
                    >
                        {error}
                    </div>
                )}

                {/* Success Message */}
                {success && (
                    <div
                        role="status"
                        className="mt-6 rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-700"
                    >
                        <p className="font-medium">
                            Class created successfully!
                        </p>

                        <p className="mt-1">
                            {success}
                        </p>
                    </div>
                )}

                {/* Buttons */}
                <div className="mt-8 flex flex-col-reverse gap-3 border-t border-gray-100 pt-6 sm:flex-row sm:justify-end">

                    <button
                        type="button"
                        onClick={() => navigate("/admin/classes")}
                        disabled={loading}
                        className="rounded-lg border border-gray-200 px-5 py-2.5 text-sm font-medium text-gray-700 transition hover:bg-gray-50 disabled:opacity-50"
                    >
                        Cancel
                    </button>

                    <button
                        type="submit"
                        disabled={loading}
                        className="rounded-lg bg-gray-900 px-6 py-2.5 text-sm font-medium text-white transition hover:bg-gray-700 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                        {loading ? "Creating..." : "Create Class"}
                    </button>

                </div>

                {/* View Classes */}
                {success && (
                    <button
                        type="button"
                        onClick={() => navigate("/admin/classes")}
                        className="mt-5 text-sm font-medium text-blue-600 hover:underline"
                    >
                        View All Classes →
                    </button>
                )}

            </form>
        </section>
    );
}

function NotFoundPage() {
    return (
        <section className="py-20 text-center">
            <h1 className="text-3xl font-semibold">404</h1>

            <p className="mt-2 text-gray-500">
                Page not found.
            </p>

            <Link to="/" className="mt-5 inline-block text-blue-600">
                Return Home
            </Link>
        </section>
    );
}

function App() {
    return (
        <BrowserRouter>
            <Routes>
                {/* Student Portal */}
                <Route element={<StudentLayout />}>
                    <Route path="/" element={<StudentHomePage />} />

                    <Route
                        path="/my-applications"
                        element={<MyApplicationsPage />}
                    />

                    <Route
                        path="/applications/:holdId"
                        element={<ApplicationDetailsPage />}
                    />
                </Route>

                {/* Admin Portal */}
                <Route path="/admin" element={<AdminLayout />}>
                    <Route index element={<AdminDashboardPage />} />

                    <Route
                        path="classes"
                        element={<AdminClassesPage />}
                    />

                    <Route
                        path="classes/create"
                        element={<CreateClassPage />}
                    />
                </Route>

                {/* Not Found */}
                <Route path="*" element={<NotFoundPage />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;
