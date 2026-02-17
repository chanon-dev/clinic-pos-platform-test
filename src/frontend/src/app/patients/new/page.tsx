"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/shared/lib/auth";
import { api } from "@/shared/lib/api";
import MainLayout from "@/shared/components/MainLayout";
import Link from "next/link";

export default function CreatePatientPage() {
  const { token } = useAuth();
  const router = useRouter();
  const [form, setForm] = useState({ firstName: "", lastName: "", phoneNumber: "", primaryBranchId: "" });
  const [branches, setBranches] = useState<{ id: string; name: string }[]>([]);
  const [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!token) return;
    api<{ data: { id: string; name: string }[] }>("/api/branches", { token })
      .then((res) => setBranches(res.data))
      .catch(() => {});
  }, [token]);

  const validate = () => {
    const errors: Record<string, string> = {};
    if (!form.firstName.trim()) errors.firstName = "First name is required";
    if (!form.lastName.trim()) errors.lastName = "Last name is required";
    if (!form.phoneNumber.trim()) errors.phoneNumber = "Phone number is required";
    else if (!/^0\d{9}$/.test(form.phoneNumber.replace(/[-\s]/g, "")))
      errors.phoneNumber = "Phone number must be 10 digits starting with 0";
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    if (!validate()) return;
    setLoading(true);

    try {
      await api("/api/patients", {
        method: "POST",
        token: token!,
        body: {
          firstName: form.firstName.trim(),
          lastName: form.lastName.trim(),
          phoneNumber: form.phoneNumber.replace(/[-\s]/g, ""),
          primaryBranchId: form.primaryBranchId || null,
        },
      });
      router.push("/patients");
    } catch (err: unknown) {
      const error = err as { detail?: string; status?: number };
      if (error.status === 409) {
        setError(error.detail || "A patient with this phone number already exists.");
      } else {
        setError(error.detail || "Failed to create patient.");
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <MainLayout>
      <div className="p-8 max-w-2xl">
        <Link href="/patients" className="text-sm text-teal-600 hover:text-teal-800 mb-4 inline-block">
          ← Back to Patients
        </Link>
        <h2 className="text-2xl font-bold text-gray-900 mb-6">New Patient</h2>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">First Name *</label>
            <input
              type="text"
              value={form.firstName}
              onChange={(e) => setForm({ ...form, firstName: e.target.value })}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
            />
            {fieldErrors.firstName && <p className="text-red-500 text-xs mt-1">{fieldErrors.firstName}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Last Name *</label>
            <input
              type="text"
              value={form.lastName}
              onChange={(e) => setForm({ ...form, lastName: e.target.value })}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
            />
            {fieldErrors.lastName && <p className="text-red-500 text-xs mt-1">{fieldErrors.lastName}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Phone Number *</label>
            <input
              type="text"
              value={form.phoneNumber}
              onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })}
              placeholder="0812345678"
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
            />
            {fieldErrors.phoneNumber && <p className="text-red-500 text-xs mt-1">{fieldErrors.phoneNumber}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Primary Branch</label>
            <select
              value={form.primaryBranchId}
              onChange={(e) => setForm({ ...form, primaryBranchId: e.target.value })}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
            >
              <option value="">Select Branch (optional)</option>
              {branches.map((b) => (
                <option key={b.id} value={b.id}>{b.name}</option>
              ))}
            </select>
          </div>
          <div className="flex gap-3 pt-4">
            <Link
              href="/patients"
              className="px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50 transition-colors text-sm"
            >
              Cancel
            </Link>
            <button
              type="submit"
              disabled={loading}
              className="bg-teal-600 text-white px-6 py-2 rounded-lg hover:bg-teal-700 disabled:opacity-50 transition-colors text-sm font-medium"
            >
              {loading ? "Saving..." : "Save Patient"}
            </button>
          </div>
        </form>
      </div>
    </MainLayout>
  );
}
