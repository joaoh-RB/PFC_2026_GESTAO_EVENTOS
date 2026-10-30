export const GroupMemberStatus = {
  Invited: 1,
  Accepted: 2,
  Declined: 3,
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