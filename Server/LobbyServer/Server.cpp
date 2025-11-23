#include "Server.h"
#include "IOCPCore.h"
#include "ClientSession.h"
#include "Logger.h"

namespace LobbyServer {

	Server::Server() :
		m_listenSocket(INVALID_SOCKET),
		m_iocpHandle(NULL),
		m_isRunning(false),
		m_iocpCore(nullptr) { }

	Server::~Server()
	{
		Stop();
	}

	bool Server::Init(uint16_t port)
	{
		if (!InitWinsock()) return false;
		if (!InitListenSocket(port)) return false;
		if (!InitIOCP()) return false;

		LOG_INFO("Server Init Success!");
		return true;
	}

	bool Server::InitWinsock()
	{
		WSADATA wsaData;
		int res = WSAStartup(MAKEWORD(2, 2), &wsaData);
		if (res != 0) LOG_ERROR("Winsock Startup Failed!"); 
		return res == 0;
	}

	bool Server::InitListenSocket(uint16_t port)
	{
		m_listenSocket = WSASocket(AF_INET, SOCK_STREAM, 0, NULL, 0, WSA_FLAG_OVERLAPPED);
		if (m_listenSocket == INVALID_SOCKET) return false;

		SOCKADDR_IN addr = {};
		addr.sin_family = AF_INET;
		addr.sin_addr.S_un.S_addr = htonl(INADDR_ANY);
		addr.sin_port = htons(port);

		if (bind(m_listenSocket, (SOCKADDR*)&addr, sizeof(addr)) == SOCKET_ERROR) 
		{
			LOG_ERROR("Listen Socket Bind Failed!");
			return false;
		}

		if (listen(m_listenSocket, SOMAXCONN) == SOCKET_ERROR) 
		{
			LOG_ERROR("Listen Socket Listen Failed!");
			return false;
		}

		LOG_INFO(std::format("Listening on PORT : {}", port));
		return true;
	}

	bool Server::InitIOCP()
	{
		m_iocpHandle = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);
		if (m_iocpHandle == NULL) 
		{
			LOG_ERROR("CreateIOCP failed!");
			return false;
		}

		m_iocpCore = new IOCPCore(m_iocpHandle);

		SYSTEM_INFO sysInfo;
		GetSystemInfo(&sysInfo);
		int threadCount = sysInfo.dwNumberOfProcessors * 2;
		
		for (int i = 0; i < threadCount; i++) 
		{
			HANDLE hThread = CreateThread(nullptr, 0, [](LPVOID param) -> DWORD {
				reinterpret_cast<Server*>(param)->WorkerLoop();
				return 0; }
			, this, 0, nullptr);
			m_workerThreads.push_back(hThread);
		}

		return true;
	}

	void Server::Run()
	{
		m_isRunning = true;

		LOG_INFO("Server Running ...");

		while (m_isRunning) 
		{
			// HACK - AcceptEx Needed
			Sleep(100);
		}
	}

	void Server::Stop()
	{
		if (!m_isRunning) return;
		m_isRunning = false;

		if (m_listenSocket != INVALID_SOCKET) 
		{
			closesocket(m_listenSocket);
			m_listenSocket = INVALID_SOCKET;
		}

		if (m_iocpHandle) 
		{
			CloseHandle(m_iocpHandle);
			m_iocpHandle = NULL;
		}

		WSACleanup();
	}


	void Server::WorkerLoop()
	{
		DWORD bytes;
		ULONG_PTR key;
		OVERLAPPED* overlapped = nullptr;

		while (m_isRunning) 
		{
			BOOL ok = GetQueuedCompletionStatus(
				m_iocpHandle,
				&bytes,
				&key,
				&overlapped,
				INFINITE
			);

			m_iocpCore->HandleCompletion(ok, bytes, key, overlapped);
		}
	}

}