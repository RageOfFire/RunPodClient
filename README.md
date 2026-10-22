# RunPod Client

WinForms .NET 8 desktop app để gửi job lên RunPod Serverless API, poll status realtime, và download ảnh/video kết quả về máy.

## Tính năng

| Tính năng | Chi tiết |
|---|---|
| Chọn endpoint | Image (SDXL pixel art) hoặc Video (Wan 2.2) |
| Gửi job | Prompt đơn giản hoặc workflow JSON đầy đủ (ComfyUI API format) |
| Poll realtime | Tự động poll status mỗi N giây |
| Download | Tự động download ảnh/video về máy khi job hoàn tất |
| Preview | Xem ảnh kết quả ngay trong app |
| Log | Log chi tiết mọi bước |
| Cancel | Huỷ job đang chạy bất cứ lúc nào |

## Yêu cầu

- Windows 10/11
- .NET 8 SDK — [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- RunPod account + API key — [runpod.io](https://www.runpod.io)
- Ít nhất 1 Serverless endpoint đã deploy

## Build & chạy

```powershell
git clone https://github.com/RageOfFire/RunPodClient.git
cd RunPodClient
dotnet run
```

## Cấu hình

Mở app → click **⚙️ Settings**:

| Field | Mô tả |
|---|---|
| RunPod API Key | Lấy tại runpod.io/console/user/settings |
| Image Endpoint ID | ID của endpoint SDXL pixel art |
| Video Endpoint ID | ID của endpoint Wan 2.2 |
| Output Folder | Thư mục lưu ảnh/video tải về |
| Poll interval | Tần suất check status (ms) |
| Timeout | Thời gian tối đa chờ job (giây) |

Settings được lưu tại `%AppData%\RunPodClient\settings.json`.

## Cách dùng

1. Chọn **Image** hoặc **Video** từ sidebar
2. Tab **Prompt**: nhập mô tả đơn giản
   — hoặc —  
   Tab **Workflow JSON**: paste workflow export từ ComfyUI (**Workflow → Export (API)**)
3. Click **▶ Submit Job**
4. App tự poll status và hiện trong Log
5. Khi xong → file tự download vào Output Folder, hiện trong danh sách Results
6. Click vào ảnh trong danh sách để preview, double-click để mở bằng app mặc định

## Liên kết

- [RunPod Serverless docs](https://docs.runpod.io/serverless/overview)
- [worker-comfyui](https://github.com/runpod-workers/worker-comfyui)
- [Docker images repo](https://github.com/RageOfFire/rof-pixel-docker)
