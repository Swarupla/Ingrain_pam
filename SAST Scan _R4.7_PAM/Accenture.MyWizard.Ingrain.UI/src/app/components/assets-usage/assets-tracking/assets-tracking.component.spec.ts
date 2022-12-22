import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { AssetsTrackingComponent } from './assets-tracking.component';

describe('AssetsTrackingComponent', () => {
  let component: AssetsTrackingComponent;
  let fixture: ComponentFixture<AssetsTrackingComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ AssetsTrackingComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(AssetsTrackingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
