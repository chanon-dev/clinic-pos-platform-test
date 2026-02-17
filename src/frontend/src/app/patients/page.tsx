"use client";

import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useAuth } from "@/shared/lib/auth";
import { api } from "@/shared/lib/api";
import MainLayout from "@/shared/components/MainLayout";
import Link from "next/link";

interface Patient {
  id: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  primaryBranch: { id: string; name: string } | null;
  createdAt: string;
}

export default function PatientsPage() {
  return (
    <Suspense fallback={<div className="p-8 text-gray-400">Loading...</div>}>
      <PatientsContent />
    </Suspense>
  );
}

function PatientsContent() {
  const { token, user } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const [patients, setPatients] = useState<Patient[]>([]);
  const [loading, setLoading] = useState(true);
  const [branches, setBranches] = useState<{ id: string; name: string }[]>([]);
  const [selectedBranch, setSelectedBranch] = useState(searchParams.get("branchId") || "");

  useEffect(() => {
    if (!token) return;
    api<{ data: { id: string; name: string }[] }>("/api/branches", { token })
      .then((res) => setBranches(res.data))
      .catch(() => {});
  }, [token]);

  useEffect(() => {
    if (!token) return;
    setLoading(true);
    const query = selectedBranch ? `?branchId=${selectedBranch}` : "";
    api<{ data: Patient[]; total: number }>(`/api/patients${query}`, { token })
      .then((res) => setPatients(res.data))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [token, selectedBranch]);

  const handleBranchChange = (branchId: string) => {
    setSelectedBranch(branchId);
    const params = new URLSearchParams();
    if (branchId) params.set("branchId", branchId);
    router.replace(`/patients${params.toString() ? `?${params}` : ""}`);
  };

  const canCreate = user?.role === "Admin" || user?.role === "User";

  return (
    <MainLayout>
      <div className="p-8">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold text-gray-900">Patients</h2>
          {canCreate && (
            <Link
              href="/patients/new"
              className="bg-teal-600 text-white px-4 py-2 rounded-lg hover:bg-teal-700 transition-colors text-sm font-medium"
            >
              + New Patient
            </Link>
          )}
        </div>

        {/* Branch Filter */}
        <div className="mb-4">
          <select
            value={selectedBranch}
            onChange={(e) => handleBranchChange(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
          >
            <option value="">All Branches</option>
            {branches.map((b) => (
              <option key={b.id} value={b.id}>{b.name}</option>
            ))}
          </select>
        </div>

        {/* Table */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          {loading ? (
            <div className="p-8 text-center text-gray-400">Loading...</div>
          ) : patients.length === 0 ? (
            <div className="p-8 text-center text-gray-400">No patients found</div>
          ) : (
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                  <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Phone</th>
                  <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Branch</th>
                  <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Created</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {patients.map((p) => (
                  <tr key={p.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 text-sm text-gray-900">{p.firstName} {p.lastName}</td>
                    <td className="px-6 py-4 text-sm text-gray-600">{p.phoneNumber}</td>
                    <td className="px-6 py-4 text-sm text-gray-600">{p.primaryBranch?.name || "-"}</td>
                    <td className="px-6 py-4 text-sm text-gray-500">{new Date(p.createdAt).toLocaleDateString("th-TH")}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        <p className="mt-3 text-sm text-gray-500">
          Showing {patients.length} patient{patients.length !== 1 ? "s" : ""}
        </p>
      </div>
    </MainLayout>
  );
}
