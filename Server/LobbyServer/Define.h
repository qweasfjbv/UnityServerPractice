#pragma once

#include <WinSock2.h>
#include <Windows.h>
#include <Ws2tcpip.h>
#include <mswsock.h>

#define MAX_SOCKBUF 1024
#define MAX_SOCK_SENDBUF 4096
#define MAX_WORKERTHREAD 4
#define RE_USE_SESSION_WAIT_TIMESEC 3

enum class IOOperation
{
	ACCEPT,
	RECV,
	SEND
};

enum class PacketType : uint16_t
{
	PKT_DUMMY = 0,
	PKT_FRIENDS,				// SEND

	PKT_PARTY_JOIN,				// RECV
	PKT_PARTY_JOIN_ACK,			// SEND
	PKT_PARTY_UPDATE,			// SEND

	PKT_PARTY_INVITE,			// RECV
	PKT_PARTY_INVITE_ACK,		// SEND
	PKT_PARTY_INVITE_NOTIFY,	// SEND
	PKT_PARTY_INVITE_ANSWER,	// RECV
	PKT_PARTY_INVITE_RESULT,	// SEND

	PKT_MATCHMAKE,
	PKT_MATCHMAKE_ACK,
	PKT_MATCH_FOUND
};

struct OverlappedEx
{
	WSAOVERLAPPED m_wsaOverlapped;
	SOCKET m_socketClient;
	WSABUF m_wsaBuf;
	IOOperation m_eOperation;
	UINT32 m_sessionIndex;
};
