import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DetDataCleanupComponent } from './det-data-cleanup.component';

describe('DetDataCleanupComponent', () => {
  let component: DetDataCleanupComponent;
  let fixture: ComponentFixture<DetDataCleanupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DetDataCleanupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DetDataCleanupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
