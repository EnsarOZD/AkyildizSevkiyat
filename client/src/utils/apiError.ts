export type ApiErrorType = 'validation_error' | 'unauthorized' | 'forbidden' | 'not_found' | 'conflict' | 'server_error' | 'network_error';

export interface ApiValidationError {
    field: string;
    message: string;
}

export interface ApiErrorResponse {
    type: ApiErrorType;
    message: string;
    errors?: ApiValidationError[];
    traceId?: string;
}

/**
 * Type guard for backend error response structure.
 * Checks for required 'type' and 'message' fields.
 */
export function isApiErrorResponse(data: unknown): data is ApiErrorResponse {
    if (!data || typeof data !== 'object') return false;
    const d = data as Record<string, unknown>;
    return (
        typeof d.type === 'string' &&
        typeof d.message === 'string'
    );
}

export class ApiError extends Error {
    public readonly type: ApiErrorType;
    public readonly errors: ApiValidationError[];
    public readonly traceId?: string;
    public readonly status?: number;

    constructor(response: ApiErrorResponse, status?: number) {
        super(response.message);
        this.name = 'ApiError';
        this.type = response.type;
        this.errors = response.errors || [];
        this.traceId = response.traceId;
        this.status = status;

        // Ensure prototype is set correctly for instanceof checks
        Object.setPrototypeOf(this, ApiError.prototype);
    }

    /**
     * Checks if the error is a validation error.
     */
    public isValidationError(): boolean {
        return this.type === 'validation_error';
    }

    /**
     * Gets error message for a specific field if it exists.
     * Performs case-insensitive matching for better tolerance.
     */
    public getFieldErrors(field: string): string[] {
        const lowerField = field.toLowerCase();
        return this.errors
            .filter(e => e.field && typeof e.field === 'string' && e.field.toLowerCase() === lowerField)
            .map(e => e.message);
    }

    /**
     * Returns a human-readable message, prioritizing validation errors if they exist.
     */
    public getFullMessage(): string {
        if (this.isValidationError() && this.errors.length > 0) {
            return `${this.message}: ${this.errors.map(e => e.message).join(', ')}`;
        }
        return this.message;
    }
}

/**
 * Safe type guard for ApiError
 */
export function isApiError(error: unknown): error is ApiError {
    return error instanceof ApiError;
}

/**
 * Static helpers for error handling
 */
export const ApiErrorUtils = {
    isValidationError(error: unknown): boolean {
        return isApiError(error) && error.isValidationError();
    },
    
    getErrorMessage(error: unknown, fallback: string = 'Beklenmedik bir hata oluştu.'): string {
        if (isApiError(error)) {
            return error.getFullMessage();
        }
        if (error instanceof Error) {
            return error.message;
        }
        return fallback;
    },

    getFieldErrors(error: unknown, field: string): string[] {
        if (isApiError(error)) {
            return error.getFieldErrors(field);
        }
        return [];
    }
};
