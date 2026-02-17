"use client";

import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useAuth } from "@/shared/lib/auth";
import { api } from "@/shared/lib/api";
import MainLayout from "@/shared/components/MainLayout";
import Link from "next/link";

interface Appointment {
  id: string;
  patient: { id: string; firstName: string; lastName: string; phoneNumber: string } | null;
  branch: { id: string; name: string } | null;
  startAt: string;
  createdAt: string;
}

export default function AppointmentsPage() {
  return (
    <Suspense fallback={<div className="p-8 text-gray-400">Loading...</div>}>
      <AppointmentsContent />
    </Suspense>
  );
}

function AppointmentsContent() {
  const { token, user } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [loading, setLoading] = useState(true);
  const [branches, setBranches] = useState<{ id: string; name: string }[]>([]);
  const [selectedBranch, setSelectedBranch] = useState(searchParams.get("branchId") || "");
  const [selectedDate, setSelectedDate] = useState(searchParams.get("date") || "");

  useEffect(() => {
    if (!token) return;
    api<{ data: { id: string; name: string }[] }>("/api/branches", { token })
      .then((res) => setBranches(res.data))
      .catch(() => {});
  }, [token]);

  useEffect(() => {
    if (!token) return;
    setLoading(true);
    const params = new URLSearchParams();
    if (selectedBranch) params.set("branchId", selectedBranch);
    if (selectedDate) params.set("date", selectedDate);
    const query = params.toString() ? `?${params}` : "";
    api<{ data: Appointment[]; total: number }>(`/api/appointments${query}`, { token })
      .then((res) => setAppointments(res.data))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [token, selectedBranch, selectedDate]);

  const handleFilterChange = (branchId: string, date: string) => {
    setSelectedBranch(branchId);
    setSelectedDate(date);
    const params = new URLSearchParams();
    if (branchId) params.set("branchId", branchId);
    if (date) params.set("date", date);
    router.replace(`/appointments${params.toString() ? `?${params}` : ""}`);
  };

  const canCreate = user?.role === "Admin" || user?.role === "User";

  return (
    <MainLayout>
      <div className="p-8">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold text-gray-900">Appointments</h2>
          {canCreate && (
            <Link
              href="/appointments/new"
              className="bg-teal-600 text-white px-4 py-2 rounded-lg hover:bg-teal-700 transition-colors text-sm font-medium"
            >
              + New Appointment
            </Link>
          )}
        </div>

        {/* Filters */}
        <div className="mb-4 flex gap-3">
          <select
            value={selectedBranch}
            onChange={(e) => handleFilterChange(e.target.value, selectedDate)}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
          >
            <option value="">All Branches</option>
            {branches.map((b) => (
              <option key={b.id} value={b.id}>{b.name}</option>
            ))}
          </select>
          <input
            type="date"
            value={selectedDate}
            onChange={(e) => handleFilterChange(selectedBranch, e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
          />
          {selectedDate && (
            <button
              onClick={() => handleFilterChange(selectedBranch, "")}
              className="text-sm text-gray-500 hover:text-gray-700"
            >
              Clear date
            </button>
          )}
        </div>

        {/* Table */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          {loading ? (
            <div className="p-8 text-center text-gray-400">Loading...</div>
          ) : appointments.length === 0 ? (
            <div className="p-8 text-center text-gray-400">No appointments found</div>
          ) : (
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Patient</th>
                  <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Phone</th>
                  <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Branch</th>
                  <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Date & Time</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {appointments.map((a) => (
                  <tr key={a.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 text-sm text-gray-900">
                      {a.patient ? `${a.patient.firstName} ${a.patient.lastName}` : "-"}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-600">
                      {a.patient?.phoneNumber || "-"}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-600">
                      {a.branch?.name || "-"}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-600">
                      {new Date(a.startAt).toLocaleString("th-TH", {
                        dateStyle: "medium",
                        timeStyle: "short",
                      })}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        <p className="mt-3 text-sm text-gray-500">
          Showing {appointments.length} appointment{appointments.length !== 1 ? "s" : ""}
        </p>
      </div>
    </MainLayout>
  );
}
