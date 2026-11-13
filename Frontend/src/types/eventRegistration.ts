import { api } from "@/services/api";
import type { PagedResult } from "@/types/utils";
import { fetchAllPages } from "@/utils/exportExcel";
import type { EventFeedbackDto } from "@/components/events/EventFeedbackModal";

export interface EventRegistrationItem {
  studentId: string;
  uniqueIdentifier: string;
  studentName: string;
  courseName: string;
  registeredAt: string;
  attended: boolean;
  attendedAt?: string | null;
  method?: number | null;
}

export const attendanceMethodLabel: Record<number, string> = {
  1: "pelo código",
  2: "manual",
};

export async function fetchEventRegistrationsPage(
  eventId: string,
  pageNumber: number = 1,
  pageSize: number = 20,
): Promise<PagedResult<EventRegistrationItem>> {
  const response = await api.get<PagedResult<EventRegistrationItem>>(
    `/events/${eventId}/registrations`,
    {
      params: { pageNumber, pageSize },
    },
  );
  return response.data;
}

export async function fetchAllEventRegistrations(
  eventId: string,
  pageSize: number = 50,
): Promise<EventRegistrationItem[]> {
  return fetchAllPages<EventRegistrationItem>(
    (pageNumber, size) =>
      fetchEventRegistrationsPage(eventId, pageNumber, size),
    pageSize,
  );
}

export async function toggleEventAttendance(
  eventId: string,
  studentId: string,
  currentlyAttended: boolean,
): Promise<void> {
  const url = `/events/${eventId}/attendance/${studentId}`;
  if (currentlyAttended) {
    await api.delete(url);
  } else {
    await api.post(url);
  }
}

export async function fetchEventFeedbacks(
  eventId: string,
): Promise<EventFeedbackDto[]> {
  const response = await api.get<EventFeedbackDto[]>(
    `/events/${eventId}/feedbacks`,
  );
  return Array.isArray(response.data) ? response.data : [];
}
