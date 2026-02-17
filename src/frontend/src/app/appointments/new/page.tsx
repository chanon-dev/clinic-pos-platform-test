"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/shared/lib/auth";
import { api } from "@/shared/lib/api";
import MainLayout from "@/shared/components/MainLayout";
import Link from "next/link";

interface Patient {
  id: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
}

export default function CreateAppointmentPage() {
  const { token } = useAuth();
  const router = useRouter();
  const [form, setForm] = useState({ patientId: "", branchId: "", startAt: "" });
  const [branches, setBranches] = useState<{ id: string; name: string }[]>([]);
  const [patients, setPatients] = useState<Patient[]>([]);
  const [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!token) return;
    api<{ data: { id: string; name: string }[] }>("/api/branches", { token })
      .then((res) => setBranches(res.data))
      .catch(() => {});
    api<{ data: Patient[] }>("/api/patients", { token })
      .then((res) => setPatients(res.data))
      .catch(() => {});
  }, [token]);

  const validate = () => {
    const errors: Record<string, string> = {};
    if (!form.patientId) errors.patientId = "Patient is required";
    if (!form.branchId) errors.branchId = "Branch is required";
    if (!form.startAt) errors.startAt = "Appointment time is required";
    else if (new Date(form.startAt) <= new Date()) errors.startAt = "Appointment time must be in the future";
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    if (!validate()) return;
    setLoading(true);

    try {
      await api("/api/appointments", {
        method: "POST",
        token: token!,
        body: {
          patientId: form.patientId,
          branchId: form.branchId,
          startAt: new Date(form.startAt).toISOString(),
        },
      });
      router.push("/appointments");
    } catch (err: unknown) {
      const error = err as { detail?: string; status?: number };
      if (error.status === 409) {
        setError(error.detail || "An appointment already exists for this patient at the same time.");
      } else {
        setError(error.detail || "Failed to create appointment.");
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <MainLayout>
      <div className="p-8 max-w-2xl">
        <Link href="/appointments" className="text-sm text-teal-600 hover:text-teal-800 mb-4 inline-block">
          &larr; Back to Appointments
        </Link>
        <h2 className="text-2xl font-bold text-gray-900 mb-6">New Appointment</h2>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Patient *</label>
            <select
              value={form.patientId}
              onChange={(e) => setForm({ ...form, patientId: e.target.value })}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
            >
              <option value="">Select Patient</option>
              {patients.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.firstName} {p.lastName} ({p.phoneNumber})
                </option>
              ))}
            </select>
            {fieldErrors.patientId && <p className="text-red-500 text-xs mt-1">{fieldErrors.patientId}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Branch *</label>
            <select
              value={form.branchId}
              onChange={(e) => setForm({ ...form, branchId: e.target.value })}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
            >
              <option value="">Select Branch</option>
              {branches.map((b) => (
                <option key={b.id} value={b.id}>{b.name}</option>
              ))}
            </select>
            {fieldErrors.branchId && <p className="text-red-500 text-xs mt-1">{fieldErrors.branchId}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Date & Time *</label>
            <input
              type="datetime-local"
              value={form.startAt}
              onChange={(e) => setForm({ ...form, startAt: e.target.value })}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
            />
            {fieldErrors.startAt && <p className="text-red-500 text-xs mt-1">{fieldErrors.startAt}</p>}
          </div>
          <div className="flex gap-3 pt-4">
            <Link
              href="/appointments"
              className="px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50 transition-colors text-sm"
            >
              Cancel
            </Link>
            <button
              type="submit"
              disabled={loading}
              className="bg-teal-600 text-white px-6 py-2 rounded-lg hover:bg-teal-700 disabled:opacity-50 transition-colors text-sm font-medium"
            >
              {loading ? "Saving..." : "Save Appointment"}
            </button>
          </div>
        </form>
      </div>
    </MainLayout>
  );
}
