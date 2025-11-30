/////////////////////////////////////////////
//
//	- Server
//
//	- Listen 시작, AcceptEx, 클라이이언트 관리
//
/////////////////////////////////////////////

#pragma once

#include <vector>
#include <WinSock2.h>
#include <Windows.h>

namespace LobbyServer {

	class IOCPCore;

	class Server
	{
	public:
		Server();
		~Server();

		bool Init(uint16_t port);
		void Run();
		void Stop();

	private:
		bool InitWinsock();
		bool InitListenSocket(uint16_t);
		bool InitIOCP();
		void WorkerLoop();

	private:
		SOCKET m_listenSocket;
		LobbyServer::IOCPCore* m_iocpCore;
	};
}