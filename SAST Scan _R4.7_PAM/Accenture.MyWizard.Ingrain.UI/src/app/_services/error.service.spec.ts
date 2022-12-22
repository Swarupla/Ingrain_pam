import { TestBed } from '@angular/core/testing';

import { ErrorsService } from './error.service'

describe('ErrorService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: ErrorsService = TestBed.get(ErrorsService);
    expect(service).toBeTruthy();
  });
});
