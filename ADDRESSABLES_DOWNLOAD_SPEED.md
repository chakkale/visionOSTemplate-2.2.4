# Addressables Download Speed Optimization

## Problem
Last 60MB downloading very slowly from GitHub releases (~3.4GB total bundle).

## Root Causes

### 1. GitHub Releases Limitations
- **Current URL**: `https://github.com/chakkale/visionOSTemplate-2.2.4/releases/download/v1.0.0`
- GitHub releases has rate limiting and bandwidth throttling
- Not optimized for large file downloads (3.4GB bundles)
- No CDN caching for release assets

### 2. Addressables Settings (Default - Not Optimized)
- Max Concurrent Requests: **3** (too low)
- Catalog Timeout: **0** (no timeout)
- Bundle Timeout: **0** (can hang on slow downloads)
- Retry Count: **0** (no auto-retry on failure)

---

## Solutions Applied

### ✅ Added Download Speed Monitoring
**File**: `InitialDownloadScene.cs`
- Real-time speed calculation (MB/s)
- Estimated time remaining
- Progress logs every second
- Shows: `Downloaded: X / Y | Speed: Z MB/s | Remaining: ~Ns`

### ✅ Performance Optimizer Tool
**Window → Addressables → Optimize Performance Settings**

Optimizations:
- Max Concurrent Requests: 3 → **10** (faster parallel downloads)
- Catalog Timeout: 0 → **30 seconds**
- Bundle Timeout: 0 → **60 seconds**
- Retry Count: 0 → **3** (auto-retry on failure)
- Redirect Limit: -1 → **5** (handle CDN redirects)

---

## Immediate Actions

### 1. Apply Performance Optimizations
```
1. Window → Addressables → Optimize Performance Settings
2. Click "Apply Optimized Settings"
3. Window → Addressables → Groups → Build → New Build
4. Upload new build to server
```

### 2. Monitor Download Speed
Run the app and check console logs:
```
[InitialDownload] Progress: 85.3% | Downloaded: 2.9GB / 3.4GB | Speed: 5.2MB/s | Remaining: 512MB (~98s)
```

If speed is consistently slow (< 1 MB/s), the issue is the hosting provider.

---

## Long-Term Solutions

### Option 1: Use a CDN (Recommended)
GitHub releases is NOT designed for large app content delivery. Use a proper CDN:

#### Cloudflare R2 (Free tier: 10GB storage, 10GB/month egress FREE)
```bash
# 1. Create Cloudflare R2 bucket
# 2. Upload ServerData/VisionOS/ folder
# 3. Get public URL: https://pub-XXXXX.r2.dev/VisionOS/

# Update Remote.LoadPath in Addressables:
https://pub-XXXXX.r2.dev/VisionOS/
```

#### AWS S3 + CloudFront
```bash
# 1. Create S3 bucket with public access
# 2. Upload ServerData/VisionOS/
# 3. Create CloudFront distribution
# 4. Update Remote.LoadPath:
https://dXXXXXXX.cloudfront.net/VisionOS/
```

#### Backblaze B2 (Cheapest: $0.005/GB/month storage)
```bash
# 1. Create B2 bucket
# 2. Enable Backblaze CDN
# 3. Upload content
# 4. Get CDN URL:
https://f000.backblazeb2.com/file/your-bucket/VisionOS/
```

### Option 2: Split Bundles by Floor
Instead of one 3.4GB bundle, create smaller bundles:
- Patio: ~300MB
- 1D Floor: ~500MB
- 1E Floor: ~400MB
- 2A Floor: ~800MB
- 3A Floor: ~600MB
- MainScene: ~100MB

Users download only the floor they visit = faster initial load.

### Option 3: Content Update Workflow
After initial 3.4GB download, use Addressables Content Update:
```
Window → Addressables → Update a Previous Build
```
This creates **delta bundles** - users only download changed content.

---

## Recommended Hosting Comparison

| Provider | Storage | Bandwidth | Speed | Cost |
|----------|---------|-----------|-------|------|
| **Cloudflare R2** | 10GB free | 10GB/month free | ⚡⚡⚡⚡⚡ | $0/month (free tier) |
| **Backblaze B2** | Unlimited | 1GB/day free | ⚡⚡⚡⚡ | $0.02/GB/month |
| **AWS S3 + CF** | 5GB free | 15GB/month free | ⚡⚡⚡⚡⚡ | $0.02/GB after free |
| **GitHub Releases** | 2GB/file | Throttled | ⚡⚡ | Free but slow |

**Verdict**: **Cloudflare R2** is best for your use case:
- 10GB free storage (enough for 3.4GB bundle)
- 10GB/month free egress (allows ~3 downloads/month)
- Global CDN with excellent speed
- No egress charges within limits

---

## How to Switch to Cloudflare R2

### 1. Create R2 Bucket
```bash
# Sign up at https://cloudflare.com
# Dashboard → R2 → Create Bucket
# Name: visionos-addressables
# Enable Public Access
```

### 2. Upload Content
```bash
# Install Wrangler CLI
npm install -g wrangler

# Authenticate
wrangler login

# Upload bundle
cd visionOSTemplate-2.2.4
wrangler r2 object put visionos-addressables/VisionOS --file=ServerData/VisionOS --recursive
```

### 3. Get Public URL
```
https://pub-XXXXXXXXXXXXX.r2.dev/VisionOS/
```

### 4. Update Addressables
```
Window → Asset Management → Addressables → Settings → Profiles
Select: "Default" or "Remote Content"
Remote.LoadPath: https://pub-XXXXXXXXXXXXX.r2.dev/VisionOS/
```

### 5. Rebuild & Test
```
Window → Addressables → Groups → Build → New Build
Run app → Should download at 10-50 MB/s (vs 1-5 MB/s on GitHub)
```

---

## Testing Download Speed

After applying optimizations, check logs for:

### Good Speed (CDN)
```
[InitialDownload] Speed: 15.3MB/s | Remaining: 512MB (~33s)
[InitialDownload] Speed: 22.1MB/s | Remaining: 100MB (~4s)
```

### Poor Speed (GitHub Releases)
```
[InitialDownload] Speed: 0.8MB/s | Remaining: 512MB (~640s)
[InitialDownload] Speed: 1.2MB/s | Remaining: 100MB (~83s)
```

If you see < 2 MB/s consistently, **switch to Cloudflare R2 or another CDN**.

---

## Summary

1. ✅ **Immediate**: Apply performance optimizations (Window → Addressables → Optimize Performance Settings)
2. ✅ **Monitor**: Check download speed logs in console
3. 🔄 **If Slow**: Switch from GitHub Releases to Cloudflare R2 (free, fast)
4. 🔄 **Optional**: Split bundles by floor for faster initial load
5. 🔄 **Updates**: Use Content Update workflow for delta downloads

**Expected Result**: 10-50 MB/s download speed (vs current 1-5 MB/s on GitHub)
