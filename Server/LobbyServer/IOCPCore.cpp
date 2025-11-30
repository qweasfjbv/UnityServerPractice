#include "IOCPCore.h"
#include "ClientSession.h"
#include "Logger.h"

namespace LobbyServer {

    IOCPCore::IOCPCore()
    {
        m_iocpHandle = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);
        if (m_iocpHandle == NULL)
        {
            LOG_ERROR("CreateIOCP failed!");
        }
    }

    IOCPCore::~IOCPCore()
    {
        if (m_iocpHandle)
        {
            CloseHandle(m_iocpHandle);
            m_iocpHandle = NULL;
        }
    }

    bool IOCPCore::InitIOCP()
    {
        SYSTEM_INFO sysInfo;
        GetSystemInfo(&sysInfo);
        int threadCount = sysInfo.dwNumberOfProcessors * 2;

        for (int i = 0; i < threadCount; i++)
        {
            HANDLE hThread = CreateThread(nullptr, 0, [](LPVOID param) -> DWORD {
                reinterpret_cast<IOCPCore*>(param)->WorkerLoop();
                return 0; }
            , this, 0, nullptr);
            m_workerThreads.push_back(hThread);
        }

        return true;
    }

    void IOCPCore::Run()
    {
        m_isRunning = true;
    }

    void IOCPCore::Stop()
    {
        m_isRunning = false;
    }

    void IOCPCore::WorkerLoop()
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

            HandleCompletion(ok, bytes, key, overlapped);
        }
    }
    
    void IOCPCore::HandleCompletion(BOOL ok, DWORD bytes, ULONG_PTR key, OVERLAPPED* overlapped)
	{
        LOG_INFO("");
        ClientSession* session = reinterpret_cast<ClientSession*>(key);
        OverlappedEx* ov = reinterpret_cast<OverlappedEx*>(overlapped);

        if (!ok || bytes == 0)
        {
            session->Disconnect();
            return;
        }

        if (ov->opType == OperationType::RECV)
            session->OnRecv(bytes, ov);

        else if (ov->opType == OperationType::SEND)
            session->OnSend(bytes, ov);
	}

    void IOCPCore::RegisterSession(ClientSession* session)
    {
        if (!session) return;

        SOCKET clientSock = session->GetSocket();
        
        HANDLE result = CreateIoCompletionPort(
            (HANDLE)clientSock,
            m_iocpHandle,
            (ULONG_PTR)session,
            0
        );

        if (result != m_iocpHandle) 
        {
            LOG_ERROR("Failed to associate client socket with IOCP.");
            closesocket(clientSock);
            return;
        }
    }
}