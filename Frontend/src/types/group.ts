export const GroupMemberStatus = {
  Invited: 1,
  Accepted: 2,
  Declined: 3,
  Requested: 4,
} as const;

export interface GroupMember {
  studentId: string;
  name: string;
  status: number;
  isLeader: boolean;
}

export interface GroupItem {
  id: string;
  eventId: string;
  eventName: string;
  number: number;
  leaderId: string;
  leaderName: string;
  members: GroupMember[];
}

export interface OpenGroupItem {
  id: string;
  number: number;
  leaderName: string;
  membersCount: number;
  maxSize?: number | null;
  availableSlots?: number | null;
  hasPendingRequest: boolean;
}