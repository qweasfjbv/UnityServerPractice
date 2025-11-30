#include "ClientSession.h"
#include "Logger.h"

namespace LobbyServer {

	ClientSession::ClientSession(SOCKET socket)
		: m_socket(socket)
	{ }

	ClientSession::~ClientSession()
	{
		Disconnect();
	}

	void ClientSession::PostRecv()
	{
		ZeroMemory(&m_recvOv.overlapped, sizeof(WSAOVERLAPPED));
		m_recvOv.wsaBuf.buf = m_recvOv.buffer;
		m_recvOv.wsaBuf.len = sizeof(m_recvOv.buffer);
		m_recvOv.opType = OperationType::RECV;

		DWORD flags = 0;

		int ret = WSARecv(
			m_socket,
			&m_recvOv.wsaBuf,
			1,
			NULL,
			&flags,
			&m_recvOv.overlapped,
			NULL
		);

		if (ret == SOCKET_ERROR)
		{
			int err = WSAGetLastError();
			if (err != WSA_IO_PENDING)
			{
				LOG_ERROR(std::format("WSARecv Error : {}", err));
				Disconnect();
			}
		}
	}

	void ClientSession::PostSend(const char* data, int len)
	{
		LOG_INFO("");
		// TODO - Dynamic Allocation Needed
		if (len > sizeof(m_sendOv.buffer))
			len = sizeof(m_sendOv.buffer);

		memcpy(m_sendOv.buffer, data, len);

		ZeroMemory(&m_sendOv.overlapped, sizeof(WSAOVERLAPPED));
		m_sendOv.wsaBuf.buf = m_sendOv.buffer;
		m_sendOv.wsaBuf.len = sizeof(m_sendOv.buffer);
		m_sendOv.opType = OperationType::SEND;

		int ret = WSASend(
			m_socket,
			&m_sendOv.wsaBuf,
			1,
			NULL,
			0,
			&m_sendOv.overlapped,
			NULL
		);

		if (ret == SOCKET_ERROR) 
		{
			int err = WSAGetLastError();
			if (err != WSA_IO_PENDING) 
			{
				LOG_ERROR(std::format("WSASend Error: {}", err));
				Disconnect();
			}
		}
	}

	void ClientSession::OnRecv(DWORD bytes, OverlappedEx* ov)
	{
		LOG_INFO("");
		if (bytes == 0) 
		{
			Disconnect();
			return;
		}

		LOG_INFO(std::format("{} {}", bytes, ov->buffer));

		// HACK - Echo Test
		PostSend(ov->buffer, bytes);

		// Re-Register next event
		PostRecv();

	}

	void ClientSession::OnSend(DWORD bytes, OverlappedEx* ov)
	{
		LOG_INFO("");
		// NOTHING TO DO
	}

	void ClientSession::Disconnect()
	{
		if (m_socket != INVALID_SOCKET)
		{
			closesocket(m_socket);
			m_socket = INVALID_SOCKET;
		}
	}
}