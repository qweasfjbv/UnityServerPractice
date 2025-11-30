/////////////////////////////////////////////
//
//	- Client Session
//
//	- 
//
/////////////////////////////////////////////

#pragma once

#include "OverlappedEx.h"

namespace LobbyServer {

	class ClientSession
	{
	public:
		ClientSession(SOCKET socket);
		~ClientSession();

		void PostRecv();
		void PostSend(const char* data, int len);

		void OnRecv(DWORD bytes, OverlappedEx* ov);
		void OnSend(DWORD bytes, OverlappedEx* ov);

		void Disconnect();

		FORCEINLINE SOCKET GetSocket() { return m_socket; }

	private:
		OverlappedEx m_recvOv;
		OverlappedEx m_sendOv;
		SOCKET m_socket;
	};
}