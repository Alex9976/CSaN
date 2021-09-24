#define _WINSOCK_DEPRECATED_NO_WARNINGS

#include <WinSock2.h>
#include <ws2tcpip.h>
#include <Ws2ipdef.h>
#include <windows.h>
#include <iostream>
#include <sysinfoapi.h>
#include <time.h>
#include <ctime>
#include <chrono>

#pragma comment(lib, "Ws2_32.lib")

using namespace std;

typedef struct IP_hdr
{
    unsigned char verhlen;
    unsigned char tos : 6;
    unsigned char additional : 2;
    unsigned short totallent;
    unsigned short id;
    unsigned short offset;
    unsigned char ttl;
    unsigned char proto;
    unsigned short checksum;
    unsigned int source;
    unsigned int destination;
} IpHeader;

typedef struct ICMP_header
{
    unsigned char i_type;
    unsigned char i_code;
    unsigned short i_checksum;
    unsigned short i_id;
    unsigned short i_seq;
} IcmpHeader;

unsigned short checksum(unsigned short* addr, int count);

unsigned int analyze(char* data, SOCKADDR_IN* adr);

int get_addr(char* host_name);

bool show_name = false;



int main(int argc, char* argv[])
{   
    bool init_adr = false;
    char* ending_adr = 0;
    
    if (argc >= 2)
    {
        ending_adr = argv[1];
        if (argc == 3)
        {
            if (strcmp(argv[2], "-n") == 0)
                show_name = true;
            else
                show_name = false;
        }
    }
    else
    {
        show_name = true;
        init_adr = true;
        ending_adr = (char*)malloc(255 * sizeof(char));
        cin >> ending_adr;
    }

    WSADATA wsd = { 0 };
    if (WSAStartup(MAKEWORD(2, 2), &wsd) != 0)
        return 1;

    int coorrect_ip = get_addr(ending_adr);
    if (coorrect_ip == -1)
        return 1;

    SOCKADDR_IN list_adr = { 0 };
    list_adr.sin_addr.S_un.S_addr = coorrect_ip;
    list_adr.sin_family = AF_INET;
    list_adr.sin_port = 0;

    SOCKADDR_IN bnd = { 0 };
    bnd.sin_addr.S_un.S_addr = htonl(INADDR_ANY);
    bnd.sin_family = AF_INET;
    bnd.sin_port = 0;

    srand(time(NULL));

    SOCKET listn = WSASocket(AF_INET, SOCK_RAW, IPPROTO_ICMP, 0, 0, WSA_FLAG_OVERLAPPED);
    bind(listn, (sockaddr*)&bnd, sizeof(bnd));
    IcmpHeader pac = { 0 };
    int timeout = 3000;
    setsockopt(listn, SOL_SOCKET, SO_RCVTIMEO, (char*)&timeout, sizeof(timeout));
    pac.i_type = 8;
    pac.i_code = 0;
    pac.i_seq = rand();
    pac.i_checksum = 0;
    pac.i_id = (USHORT)GetCurrentProcessId();
    int size = sizeof(pac) + 32;
    char* Icmp = new char[size];
    memcpy(Icmp, &pac, sizeof(pac));
    memset(Icmp + sizeof(pac), 'A', 32);

    IcmpHeader* Packet = (IcmpHeader*)Icmp;
    Packet->i_checksum = checksum((USHORT*)Packet, size);
    char buf[256] = { 0 };
    int outlent = sizeof(SOCKADDR_IN);
    SOCKADDR_IN out_ = { 0 };
    out_.sin_family = AF_INET;

    unsigned int control = list_adr.sin_addr.S_un.S_addr;
    SOCKADDR_IN buf_ = { 0 };
    int error_count = 0;
    ULONGLONG delta;
    int error;

    using namespace std::chrono;
    milliseconds time_start, time_end;
    cout << "Route to " << ending_adr << " with 30 hops\n";
    for (int i = 1; i <= 30; i++)
    {
        cout.width(3);
        cout << right << i;
        error_count = 0;
        for (int j = 1; j <= 3; j++)
        {
            pac.i_seq++;
            memcpy(Icmp, &pac, sizeof(pac));
            Packet->i_checksum = checksum((USHORT*)Packet, size);

            setsockopt(listn, IPPROTO_IP, IP_TTL, (char*)&i, 4);

            time_start = duration_cast<milliseconds>(system_clock::now().time_since_epoch());
            sendto(listn, (char*)Packet, size, 0, (sockaddr*)&list_adr, sizeof(list_adr));

            error = recvfrom(listn, buf, 256, 0, (sockaddr*)&out_, &outlent);
            time_end = duration_cast<milliseconds>(system_clock::now().time_since_epoch());
            
            

            if (error == SOCKET_ERROR)
            {
                if (WSAGetLastError() == WSAETIMEDOUT)
                {
                    cout.width(10);
                    cout << right << "*" << "   ";
                    error_count++;
                    continue;
                }   
            }
            else
                buf_ = out_;
            cout.width(10);
            delta = time_end.count() - time_start.count();
            if (delta == 0)
                cout << right << "< 1" << " ms";
            else
                cout << right << delta << " ms";
        }
        if (error_count == 3)
        {
            cout << endl;
            continue;
        }
        cout << "   ";
        if (analyze(buf, &buf_) == control)
            break;
        memset(buf, 0, 0);
    }

    delete[] Icmp;
    closesocket(listn);
    WSACleanup();

    if (init_adr)
        free(ending_adr);

    system("pause");
    return 0;
}

unsigned short checksum(unsigned short* addr, int count)
{
    long sum = 0;

    while (count > 1) {
        sum += *(unsigned short*)addr++;
        count -= 2;
    }
    if (count > 0)
        sum += *(unsigned char*)addr;
    while (sum >> 16)
        sum = (sum & 0xffff) + (sum >> 16);
    return (unsigned short)(~sum);
}

unsigned int analyze(char* data, SOCKADDR_IN* adr)
{
    char* ip;
    IpHeader* packet = (IpHeader*)data;
    ip = inet_ntoa(adr->sin_addr);
    if (show_name)
    {
        char name[NI_MAXHOST] = { 0 };
        char servInfo[NI_MAXSERV] = { 0 };

        getnameinfo((struct sockaddr*)adr, sizeof(struct sockaddr), name, NI_MAXHOST, servInfo, NI_MAXSERV, NI_NUMERICSERV);
        

        if (strcmp(ip, name) == 0)
            cout << ip << "\n";
        else
            cout << name << " [" << ip << "]" << "\n";
    }
    else
        cout << ip << "\n";

    return packet->source;
}


int get_addr(char* host_name)
{
    int result = -1;

    HOSTENT* host = gethostbyname(host_name);
    if (host)
        for (int i = 0; i < 4; i++)
            ((BYTE*)&result)[i] = host->h_addr_list[0][i];

    return result;
}