import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { UtilityApiuploadComponent } from './utility-apiupload.component';

describe('UtilityApiuploadComponent', () => {
  let component: UtilityApiuploadComponent;
  let fixture: ComponentFixture<UtilityApiuploadComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ UtilityApiuploadComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(UtilityApiuploadComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
