PROJECT_DIR := Ovk.Net

.DEFAULT_GOAL := help

.PHONY: help restore build build-web run run-https watch watch-https clean certs urls

help restore build build-web run run-https watch watch-https clean certs urls:
	$(MAKE) -C $(PROJECT_DIR) $@
