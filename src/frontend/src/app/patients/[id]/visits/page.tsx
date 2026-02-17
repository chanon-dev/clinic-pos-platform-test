"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useAuth } from "@/shared/lib/auth";
import { api } from "@/shared/lib/api";
import MainLayout from "@/shared/components/MainLayout";
import Link from "next/link";

interface Visit {
  id: string;
  patientId: string;
  branchId: string;
  branch: { id: string; name: string } | null;
  visitedAt: string;
  notes: string | null;
  createdAt: string;
}

interface Patient {
  id: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
}

export default function PatientVisitsPage() {
  const { id } = useParams<{ id: string }>();
  const { token, user } = useAuth();
  const [visits, setVisits] = useState<Visit[]>([]);
  const [patient, setPatient] = useState<Patient | null>(null);
  const [loading, setLoading] = useState(true);
  const [branches, setBranches] = useState<{ id: string; name: string }[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [formData, setFormData] = useState({ branchId: "", visitedAt: "", notes: "" });
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!token || !id) return;

    Promise.all([
      api<{ data: Visit[] }>(`/api/patients/${id}/visits`, { token }),
      api<{ data: { id: string; name: string }[] }>("/api/branches", { token }),
    ])
      .then(([visitsRes, branchesRes]) => {
        setVisits(visitsRes.data);
        setBranches(branchesRes.data);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [token, id]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSubmitting(true);

    try {
      await api(`/api/patients/${id}/visits`, {
        method: "POST",
        token: token!,
        body: {
          branchId: formData.branchId,
          visitedAt: new Date(formData.visitedAt).toISOString(),
          notes: formData.notes || null,
        },
      });
      // Refresh visits
      const res = await api<{ data: Visit[] }>(`/api/patients/${id}/visits`, { token: token! });
      setVisits(res.data);
      setShowForm(false);
      setFormData({ branchId: "", visitedAt: "", notes: "" });
    } catch (err: unknown) {
      const e = err as { detail?: string };
      setError(e.detail || "Failed to record visit");
    } finally {
      setSubmitting(false);
    }
  };

  const canCreate = user?.role === "Admin" || user?.role === "User";

  return (
    <MainLayout>
      <div className="p-8">
        <div className="flex items-center gap-3 mb-6">
          <Link href="/patients" className="text-gray-500 hover:text-gray-700 text-sm">
            Patients
          </Link>
          <span className="text-gray-400">/</span>
          <h2 className="text-2xl font-bold text-gray-900">Visit History</h2>
        </div>

        <div className="flex items-center justify-between mb-4">
          <p className="text-sm text-gray-500">{visits.length} visit{visits.length !== 1 ? "s" : ""} recorded</p>
          {canCreate && (
            <button
              onClick={() => setShowForm(!showForm)}
              className="bg-teal-600 text-white px-4 py-2 rounded-lg hover:bg-teal-700 transition-colors text-sm font-medium"
            >
              {showForm ? "Cancel" : "+ Record Visit"}
            </button>
          )}
        </div>

        {showForm && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 mb-4">
            {error && (
              <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">{error}</div>
            )}
            <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Branch</label>
                <select
                  value={formData.branchId}
                  onChange={(e) => setFormData({ ...formData, branchId: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
                  required
                >
                  <option value="">Select Branch</option>
                  {branches.map((b) => (
                    <option key={b.id} value={b.id}>{b.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Visit Date & Time</label>
                <input
                  type="datetime-local"
                  value={formData.visitedAt}
                  onChange={(e) => setFormData({ ...formData, visitedAt: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Notes (optional)</label>
                <input
                  type="text"
                  value={formData.notes}
                  onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                  placeholder="Visit notes..."
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
                />
              </div>
              <div className="md:col-span-3">
                <button
                  type="submit"
                  disabled={submitting}
                  className="bg-teal-600 text-white px-6 py-2 rounded-lg hover:bg-teal-700 disabled:opacity-50 transition-colors text-sm font-medium"
                >
                  {submitting ? "Recording..." : "Record Visit"}
                </button>
              </div>
            </form>
          </div>
        )}

        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          {loading ? (
            <div className="p-8 text-center text-gray-400">Loading...</div>
          ) : visits.length === 0 ? (
            <div className="p-8 text-center text-gray-400">No visits recorded yet</div>
          ) : (
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Visit Date</th>
                  <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Branch</th>
                  <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Notes</th>
                  <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Recorded</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {visits.map((v) => (
                  <tr key={v.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 text-sm text-gray-900">
                      {new Date(v.visitedAt).toLocaleString("th-TH")}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-600">{v.branch?.name || "-"}</td>
                    <td className="px-6 py-4 text-sm text-gray-600">{v.notes || "-"}</td>
                    <td className="px-6 py-4 text-sm text-gray-500">
                      {new Date(v.createdAt).toLocaleDateString("th-TH")}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </MainLayout>
  );
}
