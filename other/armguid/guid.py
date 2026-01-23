import uuid

def arm_guid(*args: str) -> str:
    return str(uuid.uuid5(uuid.UUID('11fb06fb-712d-4ddd-98c7-e71bbd588830'), '-'.join(args)))

# Examples:
print(arm_guid('myResourceGroup'))
print(arm_guid('myResourceGroup', 'myStorageAccount'))