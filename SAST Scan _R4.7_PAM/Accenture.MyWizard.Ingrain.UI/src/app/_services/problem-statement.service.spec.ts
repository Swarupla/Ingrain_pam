import { TestBed } from '@angular/core/testing';

import { ProblemStatementService } from './problem-statement.service';

describe('ProblemStatementService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: ProblemStatementService = TestBed.get(ProblemStatementService);
    expect(service).toBeTruthy();
  });
});
