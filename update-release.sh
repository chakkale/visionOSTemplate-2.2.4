#!/bin/bash

# update-release.sh
# Script to delete old addressable bundles from GitHub release and upload new ones
# Usage: ./update-release.sh [release-tag]

set -e  # Exit on error

# Configuration
RELEASE_TAG="${1:-v1.0.0}"
REPO="chakkale/visionOSTemplate-2.2.4"
SERVER_DATA_DIR="ServerData/VisionOS"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored messages
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if gh CLI is installed
if ! command -v gh &> /dev/null; then
    print_error "GitHub CLI (gh) is not installed. Please install it first:"
    print_error "  brew install gh"
    exit 1
fi

# Check if we're in the project directory
if [ ! -d "$SERVER_DATA_DIR" ]; then
    print_error "ServerData/VisionOS directory not found!"
    print_error "Please run this script from the project root directory."
    exit 1
fi

# Check if there are files to upload
FILE_COUNT=$(find "$SERVER_DATA_DIR" -type f | wc -l | tr -d ' ')
if [ "$FILE_COUNT" -eq 0 ]; then
    print_error "No files found in $SERVER_DATA_DIR"
    exit 1
fi

print_info "================================================"
print_info "Addressables Release Update Script"
print_info "================================================"
print_info "Release: $RELEASE_TAG"
print_info "Repository: $REPO"
print_info "Source Directory: $SERVER_DATA_DIR"
print_info "Files to upload: $FILE_COUNT"
print_info "================================================"

# Confirm before proceeding
read -p "$(echo -e ${YELLOW}Do you want to continue? [y/N]:${NC} )" -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    print_warning "Operation cancelled by user"
    exit 0
fi

# Step 1: Get list of current assets
print_info "Fetching current release assets..."
ASSET_LIST=$(gh release view "$RELEASE_TAG" --repo "$REPO" --json assets --jq '.assets[].name' | cat)

if [ -z "$ASSET_LIST" ]; then
    print_warning "No existing assets found in release $RELEASE_TAG"
    ASSET_COUNT=0
else
    ASSET_COUNT=$(echo "$ASSET_LIST" | wc -l | tr -d ' ')
    print_info "Found $ASSET_COUNT existing assets"
fi

# Step 2: Delete old assets
if [ "$ASSET_COUNT" -gt 0 ]; then
    print_info "Deleting old assets from release..."
    
    DELETED_COUNT=0
    while IFS= read -r asset_name; do
        if [ -n "$asset_name" ]; then
            print_info "  Deleting: $asset_name"
            if gh release delete-asset "$RELEASE_TAG" "$asset_name" --repo "$REPO" --yes 2>/dev/null; then
                ((DELETED_COUNT++))
            else
                print_warning "  Failed to delete: $asset_name (may not exist)"
            fi
        fi
    done <<< "$ASSET_LIST"
    
    print_success "Deleted $DELETED_COUNT assets"
    
    # Wait a moment for GitHub to process deletions
    print_info "Waiting for GitHub to process deletions..."
    sleep 3
else
    print_info "No assets to delete"
fi

# Step 3: Upload new files
print_info "Uploading new files from $SERVER_DATA_DIR..."

UPLOADED_COUNT=0
FAILED_COUNT=0

# Change to ServerData/VisionOS directory
cd "$SERVER_DATA_DIR"

# Upload each file
for file in *; do
    # Skip .DS_Store and other hidden files
    if [ -f "$file" ] && [[ ! "$file" =~ ^\. ]]; then
        print_info "  Uploading: $file"
        # Capture output for debugging
        UPLOAD_OUTPUT=$(gh release upload "$RELEASE_TAG" "$file" --repo "$REPO" --clobber 2>&1)
        UPLOAD_STATUS=$?
        
        if [ $UPLOAD_STATUS -eq 0 ]; then
            ((UPLOADED_COUNT++))
            print_success "    ✓ Uploaded successfully"
        else
            print_error "  Failed to upload: $file"
            if echo "$UPLOAD_OUTPUT" | grep -q "already exists"; then
                print_warning "    Asset already exists, trying to replace..."
                # Try deleting first then uploading
                gh release delete-asset "$RELEASE_TAG" "$file" --repo "$REPO" --yes 2>/dev/null
                sleep 1
                RETRY_OUTPUT=$(gh release upload "$RELEASE_TAG" "$file" --repo "$REPO" 2>&1)
                if [ $? -eq 0 ]; then
                    ((UPLOADED_COUNT++))
                    print_success "    ✓ Uploaded successfully on retry"
                else
                    print_error "    Retry failed: $RETRY_OUTPUT"
                    ((FAILED_COUNT++))
                fi
            else
                print_error "    Error: $UPLOAD_OUTPUT"
                ((FAILED_COUNT++))
            fi
        fi
    fi
done

# Return to project root
cd - > /dev/null

# Step 4: Summary
print_info "================================================"
print_info "Upload Summary"
print_info "================================================"
print_success "Successfully uploaded: $UPLOADED_COUNT files"

if [ "$FAILED_COUNT" -gt 0 ]; then
    print_error "Failed to upload: $FAILED_COUNT files"
    exit 1
fi

# Verify final count
print_info "Verifying release assets..."
FINAL_COUNT=$(gh release view "$RELEASE_TAG" --repo "$REPO" --json assets --jq '.assets | length' | cat)
print_success "Release now contains $FINAL_COUNT assets"

print_info "================================================"
print_success "Release update completed successfully!"
print_info "================================================"
print_info "View release at:"
print_info "https://github.com/$REPO/releases/tag/$RELEASE_TAG"
