"use client";

import { Suspense, useCallback, useEffect, useRef, useState } from "react";
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

interface PatientsResponse {
  data: Patient[];
  total: number;
  nextCursor: string | null;
  hasMore: boolean;
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
  const [loadingMore, setLoadingMore] = useState(false);
  const [branches, setBranches] = useState<{ id: string; name: string }[]>([]);
  const [selectedBranch, setSelectedBranch] = useState(searchParams.get("branchId") || "");
  const [search, setSearch] = useState(searchParams.get("search") || "");
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const [total, setTotal] = useState(0);
  const debounceRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    if (!token) return;
    api<{ data: { id: string; name: string }[] }>("/api/branches", { token })
      .then((res) => setBranches(res.data))
      .catch(() => {});
  }, [token]);

  const fetchPatients = useCallback(async (cursor?: string | null, append = false) => {
    if (!token) return;
    if (append) setLoadingMore(true); else setLoading(true);

    const params = new URLSearchParams();
    if (selectedBranch) params.set("branchId", selectedBranch);
    if (search) params.set("search", search);
    if (cursor) params.set("cursor", cursor);
    params.set("limit", "20");

    try {
      const res = await api<PatientsResponse>(`/api/patients?${params}`, { token });
      setPatients(prev => append ? [...prev, ...res.data] : res.data);
      setNextCursor(res.nextCursor);
      setHasMore(res.hasMore);
      setTotal(res.total);
    } catch {
      // ignore
    } finally {
      setLoading(false);
      setLoadingMore(false);
    }
  }, [token, selectedBranch, search]);

  useEffect(() => {
    fetchPatients();
  }, [fetchPatients]);

  const handleBranchChange = (branchId: string) => {
    setSelectedBranch(branchId);
    setPatients([]);
    const params = new URLSearchParams();
    if (branchId) params.set("branchId", branchId);
    if (search) params.set("search", search);
    router.replace(`/patients${params.toString() ? `?${params}` : ""}`);
  };

  const handleSearchChange = (value: string) => {
    setSearch(value);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      setPatients([]);
      const params = new URLSearchParams();
      if (selectedBranch) params.set("branchId", selectedBranch);
      if (value) params.set("search", value);
      router.replace(`/patients${params.toString() ? `?${params}` : ""}`);
    }, 300);
  };

  const handleLoadMore = () => {
    if (nextCursor) fetchPatients(nextCursor, true);
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

        {/* Filters */}
        <div className="flex gap-3 mb-4">
          <input
            type="text"
            value={search}
            onChange={(e) => handleSearchChange(e.target.value)}
            placeholder="Search by name or phone..."
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none w-64"
          />
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
                  <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {patients.map((p) => (
                  <tr key={p.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 text-sm text-gray-900">{p.firstName} {p.lastName}</td>
                    <td className="px-6 py-4 text-sm text-gray-600">{p.phoneNumber}</td>
                    <td className="px-6 py-4 text-sm text-gray-600">{p.primaryBranch?.name || "-"}</td>
                    <td className="px-6 py-4 text-sm text-gray-500">{new Date(p.createdAt).toLocaleDateString("th-TH")}</td>
                    <td className="px-6 py-4 text-sm">
                      <Link href={`/patients/${p.id}/visits`} className="text-teal-600 hover:text-teal-800 text-xs font-medium">
                        Visit History
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        <div className="mt-3 flex items-center justify-between">
          <p className="text-sm text-gray-500">
            Showing {patients.length} of {total} patient{total !== 1 ? "s" : ""}
          </p>
          {hasMore && (
            <button
              onClick={handleLoadMore}
              disabled={loadingMore}
              className="px-4 py-2 bg-white border border-gray-300 rounded-lg text-sm text-gray-700 hover:bg-gray-50 disabled:opacity-50 transition-colors"
            >
              {loadingMore ? "Loading..." : "Load More"}
            </button>
          )}
        </div>
      </div>
    </MainLayout>
  );
}
