import { v5 as uuidv5 } from 'uuid';

/**
 * Equivalent to Bicep function: guid(baseString [, string1, ...])
 */
function armGuid(...args: string[]): string {
    return uuidv5(args.join('-'), '11fb06fb-712d-4ddd-98c7-e71bbd588830');
}

// Examples:
console.log(armGuid('myResourceGroup'));
console.log(armGuid('myResourceGroup', 'myStorageAccount'));