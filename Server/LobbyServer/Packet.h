#pragma once

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>

#pragma pack(push, 1)

struct PacketData
{
	UINT32 m_sessionIndex = 0;
	UINT32 m_dataSize = 0;
	PacketType m_packetType;
	char* m_packetData = nullptr;

	void Set(PacketData& value) 
	{
		m_sessionIndex = value.m_sessionIndex;
		m_dataSize = value.m_dataSize;
		m_packetType = value.m_packetType;

		m_packetData = new char[value.m_dataSize];
		CopyMemory(m_packetData, value.m_packetData, value.m_dataSize);
	}

	void Set(UINT32 sessionIndex, UINT32 dataSize, char* data, PacketType packetType = PacketType::PKT_DUMMY)
	{
		m_sessionIndex = sessionIndex;
		m_dataSize = dataSize;
		m_packetType = packetType;
		
		m_packetData = new char[dataSize];
		CopyMemory(m_packetData, data, dataSize);
	}

	void Release() 
	{
		delete m_packetData;
	}
};

struct MemberInfo
{
	uint8_t m_accountId;
	char m_nickname[16];
	uint8_t m_level;
};

struct PartyMemberInfo
{
	MemberInfo m_memberInfo;
	uint8_t m_isLeader;
};

struct PKT_PartyJoinReq
{
	uint8_t m_partyId;
};

struct PKT_PartyJoinAck {};

struct PKT_PartyUpdate
{
	uint8_t partyId;
	uint8_t memberCount;
	PartyMemberInfo members[4];
};

// Inviter -> Server
struct PKT_PartyInvite
{
	uint8_t partyId;
	uint8_t targetAccountId;
};

// Server -> Inviter
struct PKT_PartyInviteAck {};

// Server -> Invitee
struct PKT_PartyInviteNotify
{
	MemberInfo m_inviter;
};

// Invitee -> Server
struct PKT_PartyInviteAnswerReq
{
	uint8_t m_partyId;
	uint8_t m_accept;	// 1 = accept, 0 = reject
};

// Server -> Inviter, Invitee (ACK)
struct PKT_PartyInviteAnswer
{
	uint8_t result; // 2 = Timeout, 1 = accept, 0 = reject
};

struct PKT_MatchMakeReq
{
	uint8_t m_partyId;
};

struct PKT_MatchMakeAck
{

};

struct PKT_MatchFound
{

};

#pragma pack(pop)