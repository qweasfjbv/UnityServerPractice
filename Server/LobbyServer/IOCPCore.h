/////////////////////////////////////////////
//
//	- IOCP Core
//
//	- 
//
/////////////////////////////////////////////

#pragma once

#include <vector>
#include <atomic>
#include <WinSock2.h>
#include <Windows.h>

namespace LobbyServer {

class ClientSession;

	class IOCPCore
	{
	public:
		IOCPCore();
		~IOCPCore();

		bool InitIOCP();
		void Run();
		void Stop();
		void RegisterSession(ClientSession* session);

		FORCEINLINE bool IsRunning() { return m_isRunning; }

	private:
		void WorkerLoop();
		void HandleCompletion(BOOL ok, DWORD bytes, ULONG_PTR key, OVERLAPPED* overlapped);
		
	private:
		std::atomic<bool> m_isRunning = false;
		std::vector<HANDLE> m_workerThreads;
		HANDLE m_iocpHandle;
	};
}