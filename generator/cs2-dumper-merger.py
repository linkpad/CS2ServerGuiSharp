#!/usr/bin/env python3
"""
Script to fetch all JSON files from cs2-dumper output directory and merge them into one JSON file.
Repository: https://github.com/a2x/cs2-dumper/tree/main/output
"""

import json
import os
import requests
from typing import Dict, Any

# GitHub API base URL
GITHUB_API_BASE = "https://api.github.com"
REPO_OWNER = "a2x"
REPO_NAME = "cs2-dumper"
OUTPUT_PATH = "output"
OUTPUT_FILE = "merged_output.json"


def get_file_contents_from_github(owner: str, repo: str, path: str) -> Dict[str, Any]:
    """
    Fetch file contents from GitHub repository using the API.
    Returns a dictionary mapping filename to JSON content.
    """
    url = f"{GITHUB_API_BASE}/repos/{owner}/{repo}/contents/{path}"
    
    print(f"Fetching file list from: {url}")
    response = requests.get(url)
    
    if response.status_code != 200:
        raise Exception(f"Failed to fetch directory: {response.status_code} - {response.text}")
    
    files = response.json()
    json_files = {}
    
    # If the response is a file (not a directory), GitHub returns a dict instead of a list
    if isinstance(files, dict):
        files = [files]
    
    for item in files:
        file_path = item.get("path", "")
        
        # Check if it's a JSON file or a directory
        if item.get("type") == "file" and file_path.endswith(".json"):
            print(f"Downloading: {file_path}")
            download_url = item.get("download_url")
            
            if download_url:
                file_response = requests.get(download_url)
                if file_response.status_code == 200:
                    try:
                        json_content = json.loads(file_response.text)
                        # Use the filename (without path) as the key
                        filename = os.path.basename(file_path)
                        json_files[filename] = json_content
                        print(f"  ✓ Successfully loaded {filename}")
                    except json.JSONDecodeError as e:
                        print(f"  ✗ Failed to parse JSON from {file_path}: {e}")
                else:
                    print(f"  ✗ Failed to download {file_path}: {file_response.status_code}")
        
        # If it's a directory, recurse into it
        elif item.get("type") == "dir":
            subdir_path = item.get("path", "")
            print(f"Exploring subdirectory: {subdir_path}")
            subdir_files = get_file_contents_from_github(owner, repo, subdir_path)
            json_files.update(subdir_files)
    
    return json_files


def deep_merge(base: Dict[str, Any], new: Dict[str, Any]) -> Dict[str, Any]:
    """
    Recursively merge two dictionaries.
    Values from 'new' will overwrite values from 'base' if keys conflict.
    """
    result = base.copy()
    
    for key, value in new.items():
        if key in result and isinstance(result[key], dict) and isinstance(value, dict):
            result[key] = deep_merge(result[key], value)
        else:
            result[key] = value
    
    return result


def merge_json_files(json_files: Dict[str, Dict[str, Any]]) -> Dict[str, Any]:
    """
    Merge multiple JSON files into a single dictionary.
    All JSON contents are merged directly into one object.
    """
    merged = {}
    
    for filename, content in sorted(json_files.items()):
        if isinstance(content, dict):
            merged = deep_merge(merged, content)
        elif isinstance(content, list):
            # If merged is empty, initialize as list, otherwise append
            if not merged:
                merged = content
            elif isinstance(merged, list):
                merged.extend(content)
            else:
                # If merged is a dict but content is a list, we can't merge directly
                # Store it with a key based on filename
                key = os.path.splitext(filename)[0]
                if key not in merged:
                    merged[key] = content
                elif isinstance(merged[key], list):
                    merged[key].extend(content)
                else:
                    merged[key] = content
        else:
            # Primitive value - store with filename as key
            key = os.path.splitext(filename)[0]
            merged[key] = content
    
    return merged


def main():
    """Main function to orchestrate the download and merge process."""
    print("=" * 60)
    print("CS2-Dumper JSON File Merger")
    print("=" * 60)
    print()
    
    try:
        # Fetch all JSON files from GitHub
        print(f"Fetching JSON files from {REPO_OWNER}/{REPO_NAME}/{OUTPUT_PATH}...")
        json_files = get_file_contents_from_github(REPO_OWNER, REPO_NAME, OUTPUT_PATH)
        
        if not json_files:
            print("No JSON files found!")
            return
        
        print()
        print(f"Found {len(json_files)} JSON file(s)")
        print()
        
        # Merge the JSON files
        print("Merging JSON files...")
        merged_data = merge_json_files(json_files)
        
        # Write the merged JSON to a file
        print(f"Writing merged data to {OUTPUT_FILE}...")
        with open(OUTPUT_FILE, 'w', encoding='utf-8') as f:
            json.dump(merged_data, f, indent=2, ensure_ascii=False)
        
        print()
        print("=" * 60)
        print(f"✓ Success! Merged {len(json_files)} file(s) into {OUTPUT_FILE}")
        print("=" * 60)
        
    except requests.exceptions.RequestException as e:
        print(f"✗ Network error: {e}")
    except Exception as e:
        print(f"✗ Error: {e}")
        import traceback
        traceback.print_exc()


if __name__ == "__main__":
    main()

