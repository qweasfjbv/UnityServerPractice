/////////////////////////////////////////////
//
//	- Server
//
//	- Listen 시작, AcceptEx, 클라이이언트 관리
//
/////////////////////////////////////////////

#pragma once

#include <WinSock2.h>
#include <Windows.h>
#include <vector>

class IOCPCore;

namespace LobbyServer {

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
		HANDLE m_iocpHandle;
		bool m_isRunning;

		IOCPCore* m_iocpCore;
		std::vector<HANDLE> m_workerThreads;
	};
}