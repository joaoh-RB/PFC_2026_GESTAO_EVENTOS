export interface DashboardOption {
  id: string;
  name: string;
}

export interface DashboardTeacherOption extends DashboardOption {
  courseIds: string[];
}

export interface DashboardEventOption extends DashboardOption {
  startDate: string;
  courseIds: string[];
  teacherIds: string[];
}

export interface DashboardSubmissionReport {
  groups: number;
  submissions: number;
  approved: number;
  rejected: number;
  pending: number;
}

export interface DashboardRatingPoint {
  rating: number;
  count: number;
}

export interface DashboardFeedbackReport {
  count: number;
  averageRating: number | null;
  distribution: DashboardRatingPoint[];
}

export interface DashboardMonthPoint {
  year: number;
  month: number;
  registrations: number;
  attendances: number;
  submissions: number;
}

export interface DashboardData {
  scopeDescription: string;
  canFilterByTeacher: boolean;
  from: string;
  to: string;
  students: number;
  pendingStudents: number;
  rejectedStudents: number;
  events: number;
  registrations: number;
  attendances: number;
  submissions: DashboardSubmissionReport;
  feedback: DashboardFeedbackReport;
  timeline: DashboardMonthPoint[];
  eventOptions: DashboardEventOption[];
  courses: DashboardOption[];
  teachers: DashboardTeacherOption[];
}
