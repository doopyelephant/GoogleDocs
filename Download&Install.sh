#!/bin/sh
install_package() {
  local package="$1"
if [ -f /etc/os-release ]; then
    . /etc/os-release
    case "$ID" in
      ubuntu|debian|raspbian)
        echo "Using APT"
        read -rp "This script will use APT to install $package, is this ok? [y/n]" accepted
if ["$accepted" = "n"]; then
          echo "This script cannot function without $package, exiting"
          exit
        fi
        if ["$accepted" = "y"]; then
          if ["$(id -u)" -ne 0]; then
            echo "Need root to install package, please rerun with sudo"
            exit
            fi
          echo "Installing using APT"
          apt install $package
          echo "$package installed"
          fi
        if ["$accepted" != "y"]; then
          echo "Invalid input, please respond with lowercase y or n, try again"
          exit
          fi
      ;;
      fedora|rhel|centos|rocky|almalinux)
        echo "Using DNF"
        read -rp "This script will use DNF to install $package, is this ok? [y/n]" accepted
        if ["$accepted" = "n"]; then
                  echo "This script cannot function without $package, exiting"
                  exit
                fi
                if ["$accepted" = "y"]; then
                  if ["$(id -u)" -ne 0]; then
                    echo "Need root to install package, please rerun with sudo"
                    exit
                    fi
                  echo "Installing using DNF"
                  dnf install $package
                  echo "$package installed"
                  fi
                if ["$accepted" != "y"]; then
                  echo "Invalid input, please respond with lowercase y or n, try again"
                  exit
                  fi
      ;;
      arch|manjaro)
        echo "Using Pacman"
        read -rp "This script will use Pacman to install $package, is this ok? [y/n]" accepted
        if ["$accepted" = "n"]; then
                  echo "This script cannot function without $package, exiting"
                  exit
                fi
                if ["$accepted" = "y"]; then
                  if ["$(id -u)" -ne 0]; then
                    echo "Need root to install package, please rerun with sudo"
                    exit
                    fi
                  echo "Installing using Pacman"
                  pacman -S $package
                  echo "$package installed"
                  fi
                if ["$accepted" != "y"]; then
                  echo "Invalid input, please respond with lowercase y or n, try again"
                  exit
                  fi
      ;;
      alpine|postmarketos)
        echo "Using APK"
        read -rp "This script will use APK to install $package, is this ok? [y/n]" accepted
        if ["$accepted" = "n"]; then
          echo "This script cannot function without $package, exiting"
          exit
        fi
        if ["$accepted" = "y"]; then
          if ["$(id -u)" -ne 0]; then
            echo "Need root to install package, please rerun with sudo"
            exit
            fi
          echo "Installing using APK"
          apk add $package
          echo "$package installed"
          fi
        if ["$accepted" != "y"]; then
          echo "Invalid input, please respond with lowercase y or n, try again"
          exit
          fi
      ;;
      *)
        echo "Your distro is not recognized by this script, please install $package with your distro's package manager and try again"
      ;;
    esac
    else
      echo "Your distro is not recognized by this script, please install $package with your distro's package manager and try again"
    fi
}

if command -v curl >/dev/null 2>&1; then
  echo "Curl is installed, continuing"
else
  echo "Curl is not installed"
  install_package "curl"
  fi
if command -v powershell >/dev/null 2>&1; then
  echo "Powershell is installed, continuing"
else
  echo "Powershell is not installed"
  install_package "powershell"
  fi
if command -v curl >/dev/null 2>&1; then
  echo "Curl Dependency satisfied"
else
  echo "Failed to satisfy dependency: curl, please install curl from your distro's package manager and try again"
  exit
  fi
if command -v powershell >/dev/null 2>&1; then
  echo "Powershell Dependency satisfied"
else
  echo "Failed to satisfy dependency: powershell, please install powershell from your distro's package manager and try again"
  exit
  fi
echo "All Dependencies Satisfied"
echo "Fetching powershell install script"
