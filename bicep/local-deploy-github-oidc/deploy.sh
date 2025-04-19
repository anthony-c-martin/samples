#!/bin/sh

this_dir=$(dirname ${BASH_SOURCE[0]})

~/.azure/bin/bicep local-deploy "$this_dir/main.bicepparam"